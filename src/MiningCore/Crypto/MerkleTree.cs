/*
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

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using MiningCore.Extensions;
using Contract = MiningCore.Contracts.Contract;

namespace MiningCore.Crypto
{
    /// <summary>
    /// Merkle tree builder.
    /// </summary>
    /// <remarks>
    /// To get a better understanding of merkle trees check: http://www.youtube.com/watch?v=gUwXCt1qkBU#t=09m09s
    /// </remarks>
    /// <specification>https://en.bitcoin.it/wiki/Protocol_specification#Merkle_Trees</specification>
    /// <example>
    /// Python implementation: http://runnable.com/U3HnDaMrJFk3gkGW/bitcoin-block-merkle-root-2-for-python
    /// Original implementation: https://code.google.com/p/bitcoinsharp/source/browse/src/Core/Block.cs#330
    /// </example>
    public class MerkleTree
    {
        /// <summary>
        /// Creates a new merkle-tree instance.
        /// </summary>
        /// <param name="hashList"></param>
        public MerkleTree(IEnumerable<byte[]> hashList)
        {
            Steps = CalculateSteps(hashList);
        }

        /// <summary>
        /// The steps in tree.
        /// </summary>
        public IList<byte[]> Steps { get; }

        /// <summary>
        /// List of hashes, will be used for calculation of merkle root.
        /// <remarks>
        ///     This is not a list of all transactions, it only contains prepared hashes of steps of merkle tree
        ///     algorithm. Please read some materials (http://en.wikipedia.org/wiki/Hash_tree) for understanding how merkle
        ///     trees calculation works. (http://mining.bitcoin.cz/stratum-mining)
        /// </remarks>
        /// <remarks>The coinbase transaction is hashed against the merkle branches to build the final merkle root.</remarks>
        /// </summary>
        public List<string> Branches
        {
            get { return Steps.Select(step => step.ToHexString()).ToList(); }
        }

        /// <summary>
        /// </summary>
        /// <example>
        /// python: http://runnable.com/U3jqtyYUmAUxtsSS/bitcoin-block-merkle-root-python
        /// nodejs: https://github.com/zone117x/node-stratum-pool/blob/master/lib/merkleTree.js#L9
        /// </example>
        /// <param name="hashList"></param>
        /// <returns></returns>
        private IList<byte[]> CalculateSteps(IEnumerable<byte[]> hashList)
        {
            Contract.RequiresNonNull(hashList, nameof(hashList));

            var steps = new List<byte[]>();

            var L = new List<byte[]> { null };
            L.AddRange(hashList);

            var startL = 2;
            var Ll = L.Count;

            if (Ll > 1)
                while(true)
                {
                    if (Ll == 1)
                        break;

                    steps.Add(L[1]);

                    if (Ll % 2 == 1)
                        L.Add(L[L.Count - 1]);

                    var Ld = new List<byte[]>();

                    //foreach (int i in Range.From(startL).To(Ll).WithStepSize(2))
                    for(var i = startL; i < Ll; i += 2)
                        Ld.Add(MerkleJoin(L[i], L[i + 1]));

                    L = new List<byte[]> { null };
                    L.AddRange(Ld);
                    Ll = L.Count;
                }

            return steps;
        }

        /// <summary>
        /// </summary>
        /// <example>
        /// nodejs: https://github.com/zone117x/node-stratum-pool/blob/master/lib/merkleTree.js#L11
        /// </example>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        private byte[] MerkleJoin(byte[] hash1, byte[] hash2)
        {
            var joined = hash1.Concat(hash2);
            var dHashed = DoubleDigest(joined).ToArray();
            return dHashed;
        }

        public byte[] WithFirst(byte[] first)
        {
            Contract.RequiresNonNull(first, nameof(first));

            foreach(var step in Steps)
                first = DoubleDigest(first.Concat(step)).ToArray();

            return first;
        }

        private static byte[] DoubleDigest(byte[] input)
        {
            using(var hash = SHA256.Create())
            {
                var first = hash.ComputeHash(input, 0, input.Length);
                return hash.ComputeHash(first);
            }
        }

        private static IEnumerable<byte> DoubleDigest(IEnumerable<byte> input)
        {
            return DoubleDigest(input.ToArray());
        }
    }
}
