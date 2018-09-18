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

using Newtonsoft.Json;

namespace MiningCore.Blockchain.Bitcoin.DaemonResponses
{
    public class TransactionDetails
    {
        public string Address { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Label { get; set; }
        public int Vout { get; set; }
    }

    public class Transaction
    {
        public decimal Amount { get; set; }
        public int Confirmations { get; set; }
        public bool Generated { get; set; }
        public string BlockHash { get; set; }
        public long BlockIndex { get; set; }
        public ulong BlockTime { get; set; }
        public string TxId { get; set; }
        public string[] WalletConflicts { get; set; }
        public ulong Time { get; set; }
        public ulong TimeReceived { get; set; }

        [JsonProperty("bip125-replaceable")]
        public string Bip125Replaceable { get; set; }

        public TransactionDetails[] Details { get; set; }
    }
}
