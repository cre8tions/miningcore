using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Miningcore.Blockchain.Bitcoin;
using Miningcore.Contracts;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Extensions;
using Miningcore.Stratum;
using Miningcore.Util;
using MoreLinq;
using NBitcoin;

namespace Miningcore.Blockchain.Ergo
{
    public class ErgoJob
    {
        public ErgoBlockTemplate BlockTemplate { get; private set; }
        public double Difficulty { get; private set; }
        public ulong Height => BlockTemplate.Work.Height;
        public string JobId { get; protected set; }

        protected object[] jobParams;
        protected readonly ConcurrentDictionary<string, bool> submissions = new(StringComparer.OrdinalIgnoreCase);
        protected static IHashAlgorithm hasher = new Blake2b();
        private Target blockTargetValue;
        private System.Numerics.BigInteger b;

        protected bool RegisterSubmit(string extraNonce1, string extraNonce2, string nTime, string nonce)
        {
            var key = new StringBuilder()
                .Append(extraNonce1)
                .Append(extraNonce2)
                .Append(nTime)
                .Append(nonce)
                .ToString();

            return submissions.TryAdd(key, true);
        }

        protected virtual byte[] SerializeCoinbase(string msg, string extraNonce1, string extraNonce2)
        {
            var msgBytes = msg.HexToByteArray();
            var extraNonce1Bytes = extraNonce1.HexToByteArray();
            var extraNonce2Bytes = extraNonce2.HexToByteArray();

            using(var stream = new MemoryStream())
            {
                stream.Write(msgBytes);
                stream.Write(extraNonce1Bytes);
                stream.Write(extraNonce2Bytes);

                return stream.ToArray();
            }
        }

        private System.Numerics.BigInteger[] GenIndexes(byte[] seed, ulong height)
        {
            var hash = new byte[32];
            hasher.Digest(seed, hash);

            // duplicate
            var extendedHash = hash.Concat(hash).ToArray();

            var result = Enumerable.Range(0, 32).Select(index =>
            {
                var a = BitConverter.ToUInt32(extendedHash.Slice(index, 4).ToArray()).ToBigEndian();
                var b = ErgoConstants.N(height);
                return a % b;
            })
            .ToArray();

            return result;
        }

        protected virtual Share ProcessShareInternal(StratumConnection worker, string extraNonce2)
        {
            var context = worker.ContextAs<ErgoWorkerContext>();
            var extraNonce1 = context.ExtraNonce1;

//extraNonce1 = "c8000001";
//extraNonce2 = "1e8ee05a";

            // build coinbase
            var coinbase = SerializeCoinbase(BlockTemplate.Work.Msg, extraNonce1, extraNonce2);

            // hash
            Span<byte> hashResult = stackalloc byte[32];
            hasher.Digest(coinbase, hashResult);

            // calculate i
            var tmp = new System.Numerics.BigInteger(hashResult.Slice(24, 8), true, true);
            var tmp2 = tmp % ErgoConstants.N(Height);
            var i = tmp2.ToByteArray(false, true).PadFront(0, 4);

            // calculate e
            var h = new System.Numerics.BigInteger(Height).ToByteArray(true, true).PadFront(0, 4);
            var ihM = i.Concat(h).Concat(ErgoConstants.M).ToArray();
            hasher.Digest(ihM, hashResult);
            var e = hashResult[1..].ToArray();

            // calculate j
            var eCoinbase = e.Concat(coinbase).ToArray();
            var jTmp = GenIndexes(eCoinbase, Height);
            var j = jTmp.Select(x => x.ToByteArray(true, true).PadFront(0, 4)).ToArray();

            // calculate f
            var fTmp = j.Select(x =>
            {
                var buf = x.Concat(h).Concat(ErgoConstants.M).ToArray();

                // hash it
                Span<byte> hash = stackalloc byte[32];
                hasher.Digest(buf, hash);

                // extract 31 bytes at end
                return new System.Numerics.BigInteger(hash[1..], true, true);
            }).ToArray();

            var f = fTmp.Aggregate((a, b) => a + b);

            // calculate fH
            var blockHash = f.ToByteArray(false, true).PadFront(0, 32);
            hasher.Digest(blockHash, hashResult);
            var fh = new System.Numerics.BigInteger(hashResult, true, true);

            // calc share-diff
            var stratumDifficulty = context.Difficulty;
            var isLowDifficultyShare = fh / new System.Numerics.BigInteger(context.Difficulty) > b;

            // check if the share meets the much harder block difficulty (block candidate)
            var isBlockCandidate = b >= fh;

            // test if share meets at least workers current difficulty
            if(!isBlockCandidate && isLowDifficultyShare)
                throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share");

            var result = new Share
            {
                BlockHeight = (long) Height,
                NetworkDifficulty = Difficulty,
                Difficulty = stratumDifficulty / ErgoConstants.DiffMultiplier
            };

            if(isBlockCandidate)
            {
                result.IsBlockCandidate = true;

                result.BlockHash = blockHash.ToHexString();
            }

            return result;
        }

        public object[] GetJobParams(bool isNew)
        {
            jobParams[^1] = isNew;
            return jobParams;
        }

        public virtual Share ProcessShare(StratumConnection worker, string extraNonce2, string nTime, string nonce)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(extraNonce2), $"{nameof(extraNonce2)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(nTime), $"{nameof(nTime)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(nonce), $"{nameof(nonce)} must not be empty");

            var context = worker.ContextAs<ErgoWorkerContext>();

            // validate nonce
            if(nonce.Length != 16)
                throw new StratumException(StratumError.Other, "incorrect size of nonce");

            // dupe check
            if(!RegisterSubmit(context.ExtraNonce1, extraNonce2, nTime, nonce))
                throw new StratumException(StratumError.DuplicateShare, "duplicate share");

            return ProcessShareInternal(worker, extraNonce2);
        }


        public void Init(ErgoBlockTemplate blockTemplate, string jobId)
        {
            BlockTemplate = blockTemplate;
            JobId = jobId;

            // Parse difficulty
            b = System.Numerics.BigInteger.Parse(BlockTemplate.Work.B, NumberStyles.Integer);
            blockTargetValue = new Target(b);
            Difficulty = blockTargetValue.Difficulty;

            jobParams = new object[]
            {
                JobId,
                Height,
                BlockTemplate.Work.Msg,
                string.Empty,
                string.Empty,
                BlockTemplate.Info.Parameters.BlockVersion,
                b,
                string.Empty,
                false
            };
        }
    }
}
