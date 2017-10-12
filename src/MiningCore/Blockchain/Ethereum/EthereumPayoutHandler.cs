﻿/* 
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial 
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using MiningCore.Blockchain.Ethereum.Configuration;
using MiningCore.Blockchain.Ethereum.DaemonRequests;
using MiningCore.Blockchain.Ethereum.DaemonResponses;
using MiningCore.Configuration;
using MiningCore.DaemonInterface;
using MiningCore.Extensions;
using MiningCore.Notifications;
using MiningCore.Payments;
using MiningCore.Persistence;
using MiningCore.Persistence.Model;
using MiningCore.Persistence.Repositories;
using MiningCore.Util;
using Newtonsoft.Json;
using Block = MiningCore.Persistence.Model.Block;
using Contract = MiningCore.Contracts.Contract;
using EC = MiningCore.Blockchain.Ethereum.EthCommands;

namespace MiningCore.Blockchain.Ethereum
{
    [CoinMetadata(CoinType.ETH, CoinType.ETC, CoinType.EXP)]
    public class EthereumPayoutHandler : PayoutHandlerBase,
        IPayoutHandler
    {
        public EthereumPayoutHandler(
            IComponentContext ctx,
            IConnectionFactory cf, 
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            NotificationService notificationService) :
            base(cf, mapper, shareRepo, blockRepo, balanceRepo, paymentRepo, notificationService)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(balanceRepo, nameof(balanceRepo));
            Contract.RequiresNonNull(paymentRepo, nameof(paymentRepo));

            this.ctx = ctx;
        }

        private readonly IComponentContext ctx;
        private DaemonClient daemon;
        private EthereumNetworkType networkType;
        private ParityChainType chainType;
        private EthereumPoolPaymentProcessingConfigExtra extraConfig;

        protected override string LogCategory => "Ethereum Payout Handler";

        #region IPayoutHandler

        public void Configure(ClusterConfig clusterConfig, PoolConfig poolConfig)
        {
            this.poolConfig = poolConfig;
            this.clusterConfig = clusterConfig;
            extraConfig = poolConfig.PaymentProcessing.Extra.SafeExtensionDataAs<EthereumPoolPaymentProcessingConfigExtra>();

            logger = LogUtil.GetPoolScopedLogger(typeof(EthereumPayoutHandler), poolConfig);

            // configure standard daemon
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            var daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            daemon = new DaemonClient(jsonSerializerSettings);
            daemon.Configure(daemonEndpoints);
        }

        public async Task<Block[]> ClassifyBlocksAsync(Block[] blocks)
        {
            Contract.RequiresNonNull(poolConfig, nameof(poolConfig));
            Contract.RequiresNonNull(blocks, nameof(blocks));

            await DetectChainAsync();

            var pageSize = 100;
            var pageCount = (int)Math.Ceiling(blocks.Length / (double)pageSize);
            var result = new List<Block>();

            var immatureCount = 0;

            for (var i = 0; i < pageCount; i++)
            {
                // get a page full of blocks
                var page = blocks
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToArray();

                // get latest block
                var latestBlockResponses = await daemon.ExecuteCmdAllAsync<DaemonResponses.Block>(EC.GetBlockByNumber, new[] {(object) "latest", true});
                var latestBlockHeight = latestBlockResponses.First(x => x.Error == null && x.Response?.Height != null).Response.Height.Value;

                // build command batch (block.TransactionConfirmationData is the hash of the blocks coinbase transaction)
                var blockBatch = page.Select(block => new DaemonCmd(EC.GetBlockByNumber,
                    new[]
                    {
                        (object) block.BlockHeight.ToStringHexWithPrefix(),
                        true
                    })).ToArray();

                // execute batch
                var responses = await daemon.ExecuteBatchAnyAsync(blockBatch);

                for (var j = 0; j < responses.Length; j++)
                {
                    var blockResponse = responses[j];

                    var blockInfo = blockResponse.Response?.ToObject<DaemonResponses.Block>();
                    var block = page[j];

                    // extract confirmation data from stored block
                    var mixHash = block.TransactionConfirmationData.Split(":").First();
                    var nonce = block.TransactionConfirmationData.Split(":").Last();

                    // check error
                    if (blockResponse.Error != null)
                    {
                        logger.Warn(() => $"[{LogCategory}] Daemon reports error '{blockResponse.Error.Message}' (Code {blockResponse.Error.Code}) for block {page[j].BlockHeight}. Will retry.");
                        continue;
                    }

                    // missing details with no error are interpreted as "orphaned"
                    if (blockInfo == null)
                    {
                        logger.Warn(() => $"[{LogCategory}] Daemon returned null result for block {page[j].BlockHeight}. Will retry.");
                        continue;
                    }

                    // don't even touch immature blocks
                    if (latestBlockHeight - block.BlockHeight < EthereumConstants.MinConfimations)
                    {
                        immatureCount++;
                        continue;
                    }

                    // mined by us?
                    if (blockInfo.Miner == poolConfig.Address)
                    {
                        // additional check
                        // NOTE: removal of first character of both sealfields caused by 
                        // https://github.com/paritytech/parity/issues/1090
                        var match = blockInfo.SealFields[0].Substring(4) == mixHash.Substring(2) && 
                                    blockInfo.SealFields[1].Substring(4) == nonce.Substring(2);

                        // confirmed
                        block.Status = BlockStatus.Confirmed;
                        block.Reward = GetBaseBlockReward(block.BlockHeight);   // base reward

                        if (extraConfig?.KeepUncles == false)
                            block.Reward += blockInfo.Uncles.Length * (block.Reward / 32); // uncle rewards

                        if (extraConfig?.KeepTransactionFees == false && blockInfo.Transactions?.Length > 0)
                            block.Reward += await GetTxRewardAsync(blockInfo); // tx fees

                        result.Add(block);

                        logger.Info(() => $"[{LogCategory}] Unlocked block {block.BlockHeight} worth {FormatAmount(block.Reward)}");
                    }

                    else
                    {
                        // don't give up yet, might be uncle
                        if (blockInfo.Uncles.Length > 0)
                        {
                            // fetch all uncles in a single RPC batch request
                            var uncleBatch = blockInfo.Uncles.Select((x, index) => new DaemonCmd(EC.GetUncleByBlockNumberAndIndex,
                                    new[] { blockInfo.Height.Value.ToStringHexWithPrefix(), index.ToStringHexWithPrefix() }))
                                .ToArray();

                            var uncleResponses = await daemon.ExecuteBatchAnyAsync(uncleBatch);

                            var uncle = uncleResponses.Where(x => x.Error == null && x.Response != null)
                                .Select(x => x.Response.ToObject<DaemonResponses.Block>())
                                .FirstOrDefault(x => x.Miner == poolConfig.Address);

                            if (uncle != null)
                            {
                                // confirmed
                                block.Status = BlockStatus.Confirmed;
                                block.Reward = GetUncleReward(uncle.Height.Value, block.BlockHeight);
                                result.Add(block);

                                logger.Info(() => $"[{LogCategory}] Unlocked uncle for block {block.BlockHeight} at height {uncle.Height.Value} worth {FormatAmount(block.Reward)}");
                                continue;
                            }

                        }

                        // we've lost this one
                        block.Status = BlockStatus.Orphaned;
                        result.Add(block);
                    }
                }
            }

            return result.ToArray();
        }

        public Task UpdateBlockRewardBalancesAsync(IDbConnection con, IDbTransaction tx, Block block, PoolConfig pool)
        {
            var blockRewardRemaining = block.Reward;

            // Distribute funds to configured reward recipients
            foreach (var recipient in poolConfig.RewardRecipients.Where(x => x.Percentage > 0))
            {
                var amount = block.Reward * (recipient.Percentage / 100.0m);
                var address = recipient.Address;

                blockRewardRemaining -= amount;

                logger.Info(() => $"Adding {FormatAmount(amount)} to balance of {address}");
                balanceRepo.AddAmount(con, tx, poolConfig.Id, poolConfig.Coin.Type, address, amount);
            }

            // Deduct static reserve for tx fees
            blockRewardRemaining -= EthereumConstants.StaticTransactionFeeReserve;

            // update block-reward
            block.Reward = blockRewardRemaining;

            return Task.FromResult(true);
        }

        public async Task PayoutAsync(Balance[] balances)
        {
            // ensure we have peers
            var infoResponse = await daemon.ExecuteCmdSingleAsync<string>(EC.GetPeerCount);
            if (infoResponse.Error != null || string.IsNullOrEmpty(infoResponse.Response) ||
                infoResponse.Response.IntegralFromHex<int>() < EthereumConstants.MinPayoutPeerCount)
            {
                logger.Warn(()=> $"[{LogCategory}] Payout aborted. Not enough peers (4 required)");
                return;
            }

            var txHashes = new List<string>();

            foreach (var balance in balances)
            {
                try
                {
                    var txHash = await Payout(balance);
                    txHashes.Add(txHash);
                }

                catch (Exception ex)
                {
                    logger.Error(() => $"[{LogCategory}] {ex.Message}");

                    NotifyPayoutFailure(new []{ balance }, ex.Message, null);
                }
            }

            if(txHashes.Any())
                NotifyPayoutSuccess(balances, txHashes.ToArray(), null);
        }

        #endregion // IPayoutHandler

        private decimal GetBaseBlockReward(ulong height)
        {
            switch (chainType)
            {
                case ParityChainType.Mainnet:
                    if (height >= EthereumConstants.ByzantiumHardForkHeight)
                        return EthereumConstants.ByzantiumBlockReward;

                    return EthereumConstants.HomesteadBlockReward;

                case ParityChainType.Classic:
                    return EthereumConstants.HomesteadBlockReward;

                case ParityChainType.Expanse:
                    return EthereumConstants.ExpanseBlockReward;

                case ParityChainType.Ropsten:
                    return EthereumConstants.ByzantiumBlockReward;

                default:
                    throw new Exception("Unable to determine block reward: Unsupported chain type");
            }
        }

        private async Task<decimal> GetTxRewardAsync(DaemonResponses.Block blockInfo)
        {
            // fetch all tx receipts in a single RPC batch request
            var batch = blockInfo.Transactions.Select(tx => new DaemonCmd(EC.GetTxReceipt, new[] { tx.Hash }))
                .ToArray();

            var results = await daemon.ExecuteBatchAnyAsync(batch);

            if(results.Any(x=> x.Error != null))
                throw new Exception($"Error fetching tx receipts: {string.Join(", ", results.Where(x=> x.Error != null).Select(y => y.Error.Message))}");

            // create lookup table
            var gasUsed = results.Select(x => x.Response.ToObject<TransactionReceipt>())
                .ToDictionary(x => x.TransactionHash, x => x.GasUsed);

            // accumulate
            var result = blockInfo.Transactions.Sum(x => (ulong) gasUsed[x.Hash] * ((decimal) x.GasPrice / EthereumConstants.Wei));

            return result;
        }

        private decimal GetUncleReward(ulong uheight, ulong height)
        {
            var reward = GetBaseBlockReward(height);

            // https://ethereum.stackexchange.com/a/27195/18000
            reward *= uheight + 8 - height;
            reward /= 8m;
            return reward;
        }

        private async Task DetectChainAsync()
        {
            var commands = new[]
            {
                new DaemonCmd(EC.GetNetVersion),
                new DaemonCmd(EC.ParityChain),
            };

            var results = await daemon.ExecuteBatchAnyAsync(commands);

            if (results.Any(x => x.Error != null))
            {
                var errors = results.Where(x => x.Error != null)
                    .ToArray();

                if (errors.Any())
                    throw new Exception($"Chain detection failed: {string.Join(", ", errors.Select(y => y.Error.Message))}");
            }

            // convert network
            var netVersion = results[0].Response.ToObject<string>();
            var parityChain = results[1].Response.ToObject<string>();

            EthereumUtils.DetectNetworkAndChain(netVersion, parityChain, out networkType, out chainType);
        }

        private async Task<string> Payout(Balance balance)
        {
            // unlock account
            if (extraConfig.CoinbasePassword != null)
            {
                var unlockResponse = await daemon.ExecuteCmdSingleAsync<object>(EC.UnlockAccount, new[]
                {
                    poolConfig.Address,
                    extraConfig.CoinbasePassword,
                    null
                });

                if(unlockResponse.Error != null || unlockResponse.Response == null || (bool) unlockResponse.Response == false)
                    throw new Exception("Unable to unlock coinbase account for sending transaction");
            }

            // send transaction
            logger.Info(() => $"[{LogCategory}] Sending {FormatAmount(balance.Amount)} to {balance.Address}");

            var request = new SendTransactionRequest
            {
                From = poolConfig.Address,
                To = balance.Address,
                Value = (ulong) Math.Floor(balance.Amount * EthereumConstants.Wei),
            };

            var response = await daemon.ExecuteCmdSingleAsync<string>(EC.SendTx, new[] { request });

            if (response.Error != null)
                throw new Exception($"{EC.SendTx} returned error: {response.Error.Message} code {response.Error.Code}");

            if (string.IsNullOrEmpty(response.Response) || EthereumConstants.ZeroHashPattern.IsMatch(response.Response))
                throw new Exception($"{EC.SendTx} did not return a valid transaction hash");

            var txHash = response.Response;
            logger.Info(() => $"[{LogCategory}] Payout transaction id: {txHash}");

            // update db
            PersistPayments(new []{ balance }, txHash);

            // done
            return txHash;
        }
    }
}
