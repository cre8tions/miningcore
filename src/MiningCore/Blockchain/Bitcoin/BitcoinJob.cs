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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using MiningCore.Blockchain.Bitcoin.DaemonResponses;
using MiningCore.Configuration;
using MiningCore.Crypto;
using MiningCore.Extensions;
using MiningCore.Stratum;
using MiningCore.Time;
using MiningCore.Util;
using NBitcoin;
using NBitcoin.DataEncoders;
using Contract = MiningCore.Contracts.Contract;
using Transaction = NBitcoin.Transaction;

namespace MiningCore.Blockchain.Bitcoin
{
    public class BitcoinJob<TBlockTemplate>
        where TBlockTemplate : BlockTemplate
    {
        protected IHashAlgorithm blockHasher;
        protected ClusterConfig clusterConfig;
        protected IMasterClock clock;
        protected IHashAlgorithm coinbaseHasher;
        protected double shareMultiplier;
        protected int extraNoncePlaceHolderLength;
        protected IHashAlgorithm headerHasher;
        protected bool isPoS;

        protected BitcoinNetworkType networkType;
        protected IDestination poolAddressDestination;
        protected PoolConfig poolConfig;
        protected HashSet<string> submissions = new HashSet<string>();
        protected BigInteger blockTargetValue;
        protected byte[] coinbaseFinal;
        protected string coinbaseFinalHex;
        protected byte[] coinbaseInitial;
        protected string coinbaseInitialHex;
        protected string[] merkleBranchesHex;
        protected MerkleTree mt;

        ///////////////////////////////////////////
        // GetJobParams related properties

        protected string previousBlockHashReversedHex;
        protected Money rewardToPool;
        protected Transaction txOut;

        // serialization constants
        protected static byte[] scriptSigFinalBytes = new Script(Op.GetPushOp(Encoding.UTF8.GetBytes("/MiningCore/"))).ToBytes();

        protected static byte[] sha256Empty = Enumerable.Repeat((byte) 0, 32).ToArray();
        protected static uint txVersion = 1u; // transaction version (currently 1) - see https://en.bitcoin.it/wiki/Transaction

        protected static uint txInputCount = 1u;
        protected static uint txInPrevOutIndex = (uint) (Math.Pow(2, 32) - 1);
        protected static uint txInSequence;
        protected static uint txLockTime;

        protected virtual void BuildMerkleBranches()
        {
            var transactionHashes = BlockTemplate.Transactions
                .Select(tx => (tx.TxId ?? tx.Hash)
                    .HexToByteArray()
                    .ReverseArray())
                .ToArray();

            mt = new MerkleTree(transactionHashes);

            merkleBranchesHex = mt.Steps
                .Select(x => x.ToHexString())
                .ToArray();
        }

        protected virtual void BuildCoinbase()
        {
            var extraNoncePlaceHolderLengthByte = (byte) extraNoncePlaceHolderLength;

            // generate script parts
            var sigScriptInitial = GenerateScriptSigInitial();
            var sigScriptInitialBytes = sigScriptInitial.ToBytes();

            var sigScriptLength = (uint) (
                sigScriptInitial.Length +
                1 + // for extranonce-placeholder length after sigScriptInitial
                extraNoncePlaceHolderLength +
                scriptSigFinalBytes.Length);

            // output transaction
            txOut = CreateOutputTransaction();

            // build coinbase initial
            using(var stream = new MemoryStream())
            {
                var bs = new BitcoinStream(stream, true);

                // version
                bs.ReadWrite(ref txVersion);

                // timestamp for POS coins
                if (isPoS)
                {
                    var timestamp = BlockTemplate.CurTime;
                    bs.ReadWrite(ref timestamp);
                }

                // serialize (simulated) input transaction
                bs.ReadWriteAsVarInt(ref txInputCount);
                bs.ReadWrite(ref sha256Empty);
                bs.ReadWrite(ref txInPrevOutIndex);

                // signature script initial part
                bs.ReadWriteAsVarInt(ref sigScriptLength);
                bs.ReadWrite(ref sigScriptInitialBytes);

                // emit a simulated OP_PUSH(n) just without the payload (which is filled in by the miner: extranonce1 and extranonce2)
                bs.ReadWrite(ref extraNoncePlaceHolderLengthByte);

                // done
                coinbaseInitial = stream.ToArray();
                coinbaseInitialHex = coinbaseInitial.ToHexString();
            }

            // build coinbase final
            using(var stream = new MemoryStream())
            {
                var bs = new BitcoinStream(stream, true);

                // signature script final part
                bs.ReadWrite(ref scriptSigFinalBytes);

                // tx in sequence
                bs.ReadWrite(ref txInSequence);

                // serialize output transaction
                var txOutBytes = SerializeOutputTransaction(txOut);
                bs.ReadWrite(ref txOutBytes);

                // misc
                bs.ReadWrite(ref txLockTime);

                // done
                coinbaseFinal = stream.ToArray();
                coinbaseFinalHex = coinbaseFinal.ToHexString();
            }
        }

        protected virtual byte[] SerializeOutputTransaction(Transaction tx)
        {
            var withDefaultWitnessCommitment = !string.IsNullOrEmpty(BlockTemplate.DefaultWitnessCommitment);

            var outputCount = (uint) tx.Outputs.Count;
            if (withDefaultWitnessCommitment)
                outputCount++;

            using(var stream = new MemoryStream())
            {
                var bs = new BitcoinStream(stream, true);

                // write output count
                bs.ReadWriteAsVarInt(ref outputCount);

                long amount;
                byte[] raw;
                uint rawLength;

                // serialize witness
                if (withDefaultWitnessCommitment)
                {
                    amount = 0;
                    raw = BlockTemplate.DefaultWitnessCommitment.HexToByteArray();
                    rawLength = (uint) raw.Length;

                    bs.ReadWrite(ref amount);
                    bs.ReadWriteAsVarInt(ref rawLength);
                    bs.ReadWrite(ref raw);
                }

                // serialize other recipients
                foreach(var output in tx.Outputs)
                {
                    amount = output.Value.Satoshi;
                    var outScript = output.ScriptPubKey;
                    raw = outScript.ToBytes(true);
                    rawLength = (uint) raw.Length;

                    bs.ReadWrite(ref amount);
                    bs.ReadWriteAsVarInt(ref rawLength);
                    bs.ReadWrite(ref raw);
                }

                return stream.ToArray();
            }
        }

        protected virtual Script GenerateScriptSigInitial()
        {
            var now = ((DateTimeOffset) clock.UtcNow).ToUnixTimeSeconds();

            // script ops
            var ops = new List<Op>();

            // push block height
            ops.Add(Op.GetPushOp(BlockTemplate.Height));

            // optionally push aux-flags
            if (!string.IsNullOrEmpty(BlockTemplate.CoinbaseAux?.Flags))
                ops.Add(Op.GetPushOp(BlockTemplate.CoinbaseAux.Flags.HexToByteArray()));

            // push timestamp
            ops.Add(Op.GetPushOp(now));

            return new Script(ops);
        }

        protected virtual Transaction CreateOutputTransaction()
        {
            rewardToPool = new Money(BlockTemplate.CoinbaseValue, MoneyUnit.Satoshi);

            var tx = new Transaction();

            tx.Outputs.Insert(0, new TxOut(rewardToPool, poolAddressDestination)
            {
                Value = rewardToPool
            });

            return tx;
        }

        protected bool RegisterSubmit(string extraNonce1, string extraNonce2, string nTime, string nonce)
        {
            lock(submissions)
            {
                var key = extraNonce1 + extraNonce2 + nTime + nonce;
                if (submissions.Contains(key))
                    return false;

                submissions.Add(key);
                return true;
            }
        }

        protected virtual byte[] SerializeHeader(byte[] coinbaseHash, uint nTime, uint nonce)
        {
            // build merkle-root
            var merkleRoot = mt.WithFirst(coinbaseHash);

            var blockHeader = new BlockHeader
            {
                Version = (int) BlockTemplate.Version,
                Bits = new Target(Encoders.Hex.DecodeData(BlockTemplate.Bits)),
                HashPrevBlock = uint256.Parse(BlockTemplate.PreviousBlockhash),
                HashMerkleRoot = new uint256(merkleRoot),
                BlockTime = DateTimeOffset.FromUnixTimeSeconds(nTime),
                Nonce = nonce
            };

            return blockHeader.ToBytes();
        }

        protected virtual BitcoinShare ProcessShareInternal(StratumClient<BitcoinWorkerContext> worker, string extraNonce2, uint nTime, uint nonce)
        {
            var extraNonce1 = worker.Context.ExtraNonce1;

            // build coinbase
            var coinbase = SerializeCoinbase(extraNonce1, extraNonce2);
            var coinbaseHash = coinbaseHasher.Digest(coinbase);

            // hash block-header
            var headerBytes = SerializeHeader(coinbaseHash, nTime, nonce);
            var headerHash = headerHasher.Digest(headerBytes, (ulong) nTime);
            var headerValue = BigInteger.Parse("00" + headerHash.ReverseArray().ToHexString(), NumberStyles.HexNumber);

            // calc share-diff
            var shareDiff = (double) new BigRational(BitcoinConstants.Diff1, headerValue) * shareMultiplier;
            var stratumDifficulty = worker.Context.Difficulty;
            var ratio = shareDiff / stratumDifficulty;

            // check if the share meets the much harder block difficulty (block candidate)
            var isBlockCandidate = headerValue < blockTargetValue;

            // test if share meets at least workers current difficulty
            if (!isBlockCandidate && ratio < 0.99)
            {
                // check if share matched the previous difficulty from before a vardiff retarget
                if (worker.Context.VarDiff?.LastUpdate != null && worker.Context.PreviousDifficulty.HasValue)
                {
                    ratio = shareDiff / worker.Context.PreviousDifficulty.Value;

                    if (ratio < 0.99)
                        throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({shareDiff})");

                    // use previous difficulty
                    stratumDifficulty = worker.Context.PreviousDifficulty.Value;
                }

                else
                    throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({shareDiff})");
            }

            var result = new BitcoinShare
            {
                BlockHeight = BlockTemplate.Height,
                IsBlockCandidate = isBlockCandidate
            };

            var blockBytes = SerializeBlock(headerBytes, coinbase);
            result.BlockHex = blockBytes.ToHexString();
            result.BlockHash = blockHasher.Digest(headerBytes, nTime).ToHexString();
            result.BlockHeight = BlockTemplate.Height;
            result.BlockReward = rewardToPool.ToDecimal(MoneyUnit.BTC);
            result.Difficulty = stratumDifficulty;

            return result;
        }

        protected virtual byte[] SerializeCoinbase(string extraNonce1, string extraNonce2)
        {
            var extraNonce1Bytes = extraNonce1.HexToByteArray();
            var extraNonce2Bytes = extraNonce2.HexToByteArray();

            using(var stream = new MemoryStream())
            {
                stream.Write(coinbaseInitial);
                stream.Write(extraNonce1Bytes);
                stream.Write(extraNonce2Bytes);
                stream.Write(coinbaseFinal);

                return stream.ToArray();
            }
        }

        protected virtual byte[] SerializeBlock(byte[] header, byte[] coinbase)
        {
            var transactionCount = (uint) BlockTemplate.Transactions.Length + 1; // +1 for prepended coinbase tx
            var rawTransactionBuffer = BuildRawTransactionBuffer();

            using(var stream = new MemoryStream())
            {
                var bs = new BitcoinStream(stream, true);

                bs.ReadWrite(ref header);
                bs.ReadWriteAsVarInt(ref transactionCount);
                bs.ReadWrite(ref coinbase);
                bs.ReadWrite(ref rawTransactionBuffer);

                // TODO: handle DASH coin masternode_payments

                // POS coins require a zero byte appended to block which the daemon replaces with the signature
                if (isPoS)
                    bs.ReadWrite((byte) 0);

                return stream.ToArray();
            }
        }

        protected virtual byte[] BuildRawTransactionBuffer()
        {
            using(var stream = new MemoryStream())
            {
                foreach(var tx in BlockTemplate.Transactions)
                {
                    var txRaw = tx.Data.HexToByteArray();
                    stream.Write(txRaw);
                }

                return stream.ToArray();
            }
        }

        #region API-Surface

        public TBlockTemplate BlockTemplate { get; protected set; }
        public double Difficulty { get; protected set; }

        public string JobId { get; protected set; }

        public virtual void Init(TBlockTemplate blockTemplate, string jobId,
            PoolConfig poolConfig, ClusterConfig clusterConfig, IMasterClock clock,
            IDestination poolAddressDestination, BitcoinNetworkType networkType,
            bool isPoS, double shareMultiplier,
            IHashAlgorithm coinbaseHasher, IHashAlgorithm headerHasher, IHashAlgorithm blockHasher)
        {
            Contract.RequiresNonNull(blockTemplate, nameof(blockTemplate));
            Contract.RequiresNonNull(poolConfig, nameof(poolConfig));
            Contract.RequiresNonNull(clusterConfig, nameof(clusterConfig));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(poolAddressDestination, nameof(poolAddressDestination));
            Contract.RequiresNonNull(coinbaseHasher, nameof(coinbaseHasher));
            Contract.RequiresNonNull(headerHasher, nameof(headerHasher));
            Contract.RequiresNonNull(blockHasher, nameof(blockHasher));
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(jobId), $"{nameof(jobId)} must not be empty");

            this.poolConfig = poolConfig;
            this.clusterConfig = clusterConfig;
            this.clock = clock;
            this.poolAddressDestination = poolAddressDestination;
            this.networkType = networkType;
            BlockTemplate = blockTemplate;
            JobId = jobId;
            Difficulty = new Target(new NBitcoin.BouncyCastle.Math.BigInteger(BlockTemplate.Target, 16)).Difficulty;

            extraNoncePlaceHolderLength = BitcoinExtraNonceProvider.PlaceHolder.Length;
            this.isPoS = isPoS;
            this.shareMultiplier = shareMultiplier;

            this.coinbaseHasher = coinbaseHasher;
            this.headerHasher = headerHasher;
            this.blockHasher = blockHasher;

            blockTargetValue = BigInteger.Parse(BlockTemplate.Target, NumberStyles.HexNumber);

            previousBlockHashReversedHex = BlockTemplate.PreviousBlockhash
                .HexToByteArray()
                .ReverseByteOrder()
                .ToHexString();

            BuildMerkleBranches();
            BuildCoinbase();
        }

        public virtual object GetJobParams(bool isNew)
        {
            return new object[]
            {
                JobId,
                previousBlockHashReversedHex,
                coinbaseInitialHex,
                coinbaseFinalHex,
                merkleBranchesHex,
                BlockTemplate.Version.ToStringHex8(),
                BlockTemplate.Bits,
                BlockTemplate.CurTime.ToStringHex8(),
                isNew
            };
        }

        public virtual BitcoinShare ProcessShare(StratumClient<BitcoinWorkerContext> worker,
            string extraNonce2, string nTime, string nonce)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(extraNonce2), $"{nameof(extraNonce2)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(nTime), $"{nameof(nTime)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(nonce), $"{nameof(nonce)} must not be empty");

            // validate nTime
            if (nTime.Length != 8)
                throw new StratumException(StratumError.Other, "incorrect size of ntime");

            var nTimeInt = uint.Parse(nTime, NumberStyles.HexNumber);
            if (nTimeInt < BlockTemplate.CurTime || nTimeInt > ((DateTimeOffset) clock.UtcNow).ToUnixTimeSeconds() + 7200)
                throw new StratumException(StratumError.Other, "ntime out of range");

            // validate nonce
            if (nonce.Length != 8)
                throw new StratumException(StratumError.Other, "incorrect size of nonce");

            var nonceInt = uint.Parse(nonce, NumberStyles.HexNumber);

            // dupe check
            if (!RegisterSubmit(worker.Context.ExtraNonce1, extraNonce2, nTime, nonce))
                throw new StratumException(StratumError.DuplicateShare, "duplicate share");

            return ProcessShareInternal(worker, extraNonce2, nTimeInt, nonceInt);
        }

        #endregion // API-Surface
    }
}
