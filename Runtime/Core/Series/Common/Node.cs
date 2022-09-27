namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal struct Node<TBounds, TVector>
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private const int Null = SpatialTree<System.Object, TBounds, TVector>.Null;

        public int parentIndex;
        public int firstChildIndex;
        public int objectsCount;
        public bool isLeaf;
        
        public byte depth;
        public TBounds bounds;

        public Node(byte depth, TBounds bounds)
        {
            parentIndex = Null;
            firstChildIndex = Null;
            objectsCount = 0;
            isLeaf = true;
            this.depth = depth;
            this.bounds = bounds;
        }
    }
}
