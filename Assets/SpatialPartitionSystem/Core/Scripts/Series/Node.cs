namespace SpatialPartitionSystem.Core.Series
{
    internal struct Node
    {
        public int firstChildIndex;
        public ushort objectsCount;
        public bool isLeaf;
        
        public sbyte depth;
        public AABB2D bounds;
    }
}
