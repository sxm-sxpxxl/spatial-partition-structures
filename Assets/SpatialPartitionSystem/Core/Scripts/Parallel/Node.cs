namespace SpatialPartitionSystem.Core.Parallel
{
    internal struct Node
    {
        // Points to this node's first child index in elements
        public int firstChildIndex;

        // Number of objects in the leaf
        public short objectsCount;
        public bool isLeaf;
    }
}
