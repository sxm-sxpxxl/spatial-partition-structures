namespace SpatialPartitionSystem.Core.Series
{
    internal struct Node
    {
        // Points to this node's first child index in object pointers if the node is leaf;
        // Points to this node's first child index in nodes if the node if branch.
        public int firstChildIndex;
        public short objectsCount;
        public bool isLeaf;
    }
}
