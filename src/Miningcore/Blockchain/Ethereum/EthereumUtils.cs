using System;

namespace Miningcore.Blockchain.Ethereum
{
    public class EthereumUtils
    {
        public static void DetectNetworkAndChain(string netVersionResponse, string gethChainResponse,
            out EthereumNetworkType networkType, out GethChainType chainType)
        {
            // convert network
            if(int.TryParse(netVersionResponse, out var netWorkTypeInt))
            {
                networkType = (EthereumNetworkType) netWorkTypeInt;

                if(!Enum.IsDefined(typeof(EthereumNetworkType), networkType))
                    networkType = EthereumNetworkType.Unknown;
            }

            else
                networkType = EthereumNetworkType.Unknown;

            // convert chain
            if(!Enum.TryParse(gethChainResponse, true, out chainType))
            {
               chainType = GethChainType.Unknown;
            }

            if(chainType == GethChainType.Mainnet)
                chainType = GethChainType.Mainnet;
        }
    }
}
