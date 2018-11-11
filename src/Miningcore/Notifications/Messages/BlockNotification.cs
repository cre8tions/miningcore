using Miningcore.Persistence.Model;

namespace Miningcore.Notifications.Messages
{
    public abstract class BlockNotification
    {
        public string PoolId { get; set; }
        public ulong BlockHeight { get; set; }
        public string Symbol { get; set; }
    }

    public class BlockFoundNotification : BlockNotification
    {
        public string Miner { get; set; }
        public string MinerExplorerLink { get; set; }
    }

    public class NewChainHeightNotification : BlockNotification
    {
    }

    public class BlockConfirmationProgressNotification : BlockNotification
    {
        public double Progress { get; set; }
    }

    public class BlockUnlockedNotification : BlockNotification
    {
        public BlockStatus Status { get; set; }
        public string BlockType { get; set; }
        public string BlockHash { get; set; }
        public string Miner { get; set; }
        public string ExplorerLink { get; set; }
        public string MinerExplorerLink { get; set; }
    }
}
