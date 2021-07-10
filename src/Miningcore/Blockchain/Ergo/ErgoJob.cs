using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Miningcore.Contracts;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Extensions;
using Miningcore.Stratum;
using System.Numerics;
using NBitcoin;

namespace Miningcore.Blockchain.Ergo
{
    public class ErgoJob
    {
        public ErgoBlockTemplate BlockTemplate { get; private set; }
        public double Difficulty => bTarget.Difficulty;
        public uint Height => BlockTemplate.Work.Height;
        public string JobId { get; protected set; }

        private object[] jobParams;
        private readonly ConcurrentDictionary<string, bool> submissions = new(StringComparer.OrdinalIgnoreCase);
        private static readonly IHashAlgorithm hasher = new Blake2b();
        private int extraNonceSize;

        private static readonly uint nBase = (uint) Math.Pow(2, 26);
        private Target bTarget;
        private BigInteger b;
        private const uint IncreaseStart = 600 * 1024;
        private const uint IncreasePeriodForN = 50 * 1024;
        private const uint NIncreasementHeightMax = 9216000;

        public static uint CalcN(uint height)
        {
            height = Math.Min(NIncreasementHeightMax, height);

            switch (height)
            {
                case < IncreaseStart:
                    return nBase;
                case >= NIncreasementHeightMax:
                    return 2147387550;
            }

            var step = nBase;
            var iterationsNumber = (height - IncreaseStart) / IncreasePeriodForN + 1;

            for(var i = 0; i < iterationsNumber; i++)
                step = step / 100 * 105;

            return step;
        }

        protected bool RegisterSubmit(string nTime, string nonce)
        {
            var key = new StringBuilder()
                .Append(nTime)
                .Append(nonce)
                .ToString();

            return submissions.TryAdd(key, true);
        }

        protected virtual byte[] SerializeCoinbase(string msg, string nonce)
        {
            using(var stream = new MemoryStream())
            {
                stream.Write(msg.HexToByteArray());
                stream.Write(nonce.HexToByteArray());

                return stream.ToArray();
            }
        }

        private BigInteger[] GenIndexes(byte[] seed, uint height)
        {
            // hash seed
            Span<byte> hash = stackalloc byte[32];
            hasher.Digest(seed, hash);

            // duplicate
            Span<byte> extendedHash = stackalloc byte[64];
            hash.CopyTo(extendedHash);
            hash.CopyTo(extendedHash.Slice(32, 32));

            // map indexes
            var result = new BigInteger[32];

            for(var i = 0; i < 32; i++)
            {
                var x = BitConverter.ToUInt32(extendedHash.Slice(i, 4)).ToBigEndian();
                var y = CalcN(height);
                result[i] = x % y;
            }

            return result;
        }

        protected virtual Share ProcessShareInternal(StratumConnection worker, string nonce)
        {
            var context = worker.ContextAs<ErgoWorkerContext>();

            // hash coinbase
            var coinbase = SerializeCoinbase(BlockTemplate.Work.Msg, nonce);
            Span<byte> hashResult = stackalloc byte[32];
            hasher.Digest(coinbase, hashResult);

            // calculate i
            var slice = hashResult.Slice(24, 8);
            var tmp2 = new BigInteger(slice, true, true) % CalcN(Height);
            var i = tmp2.ToByteArray(false, true).PadFront(0, 4);

            // calculate e
            var h = new BigInteger(Height).ToByteArray(true, true).PadFront(0, 4);
            var ihM = i.Concat(h).Concat(ErgoConstants.M).ToArray();
            hasher.Digest(ihM, hashResult);
            var e = hashResult[1..].ToArray();

            // calculate j
            var eCoinbase = e.Concat(coinbase).ToArray();
            var jTmp = GenIndexes(eCoinbase, Height);
            var j = jTmp.Select(x => x.ToByteArray(true, true).PadFront(0, 4)).ToArray();

            // calculate f
            var f = j.Select(x =>
            {
                var buf = x.Concat(h).Concat(ErgoConstants.M).ToArray();

                // hash it
                Span<byte> hash = stackalloc byte[32];
                hasher.Digest(buf, hash);

                // extract 31 bytes at end
                return new BigInteger(hash[1..], true, true);
            }).Aggregate((x, y) => x + y);

            // calculate fH
            var blockHash = f.ToByteArray(true, true).PadFront(0, 32);
            hasher.Digest(blockHash, hashResult);
            var fh = new BigInteger(hashResult, true, true);
            var fhTarget = new Target(fh);

            // diff check
            var stratumDifficulty = context.Difficulty;
            var ratio = fhTarget.Difficulty / stratumDifficulty;

            // check if the share meets the much harder block difficulty (block candidate)
            var isBlockCandidate = fh < b;

            // test if share meets at least workers current difficulty
            if(!isBlockCandidate && ratio < 0.99)
            {
                // check if share matched the previous difficulty from before a vardiff retarget
                if(context.VarDiff?.LastUpdate != null && context.PreviousDifficulty.HasValue)
                {
                    ratio = fhTarget.Difficulty / context.PreviousDifficulty.Value;

                    if(ratio < 0.99)
                        throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({fhTarget.Difficulty})");

                    // use previous difficulty
                    stratumDifficulty = context.PreviousDifficulty.Value;
                }

                else
                    throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({fhTarget.Difficulty})");
            }

            var result = new Share
            {
                BlockHeight = Height,
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
            if(nonce.Length != context.ExtraNonce1.Length + extraNonceSize * 2)
                throw new StratumException(StratumError.Other, "incorrect size of nonce");

            if(!nonce.StartsWith(context.ExtraNonce1))
                throw new StratumException(StratumError.Other, $"incorrect extraNonce2 in nonce (expected {context.ExtraNonce1}, got {nonce.Substring(0, Math.Min(nonce.Length, context.ExtraNonce1.Length))})");

            // currently unused
            if(nTime == "undefined")
                nTime = string.Empty;

            // dupe check
            if(!RegisterSubmit(nTime, nonce))
                throw new StratumException(StratumError.DuplicateShare, $"duplicate share");

            return ProcessShareInternal(worker, nonce);
        }

        public void Init(ErgoBlockTemplate blockTemplate, int blockVersion, int extraNonceSize, string jobId)
        {
            this.extraNonceSize = extraNonceSize;

            BlockTemplate = blockTemplate;
            JobId = jobId;
            b = BigInteger.Parse(BlockTemplate.Work.B, NumberStyles.Integer);
            bTarget = new Target(b);

            jobParams = new object[]
            {
                JobId,
                Height,
                BlockTemplate.Work.Msg,
                string.Empty,
                string.Empty,
                blockVersion,
                null,   // to filled out by ErgoPool.SendJob
                string.Empty,
                false
            };
        }
    }
}
