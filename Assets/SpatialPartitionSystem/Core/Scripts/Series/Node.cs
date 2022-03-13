namespace SpatialPartitionSystem.Core.Series
{
    internal struct Node
    {
        public int firstChildIndex;
        public byte objectsCount;
        public bool isLeaf;
        
        public byte depth;
        public AABB2D bounds;
    }
}
