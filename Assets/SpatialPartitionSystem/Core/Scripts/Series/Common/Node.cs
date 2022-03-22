namespace SpatialPartitionSystem.Core.Series
{
    internal struct Node<TBounds, TVector>
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public int parentIndex;
        public int firstChildIndex;
        public byte objectsCount;
        public bool isLeaf;
        
        public byte depth;
        public TBounds bounds;
    }
}
