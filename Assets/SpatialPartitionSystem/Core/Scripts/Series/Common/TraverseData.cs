namespace SpatialPartitionSystem.Core.Series
{
    internal struct TraverseData<TBounds, TVector>
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public int nodeIndex;
        public Node<TBounds, TVector> node;
            
        public bool isParentChanged;
        public bool isLastChild;

        public TraverseData(int nodeIndex, Node<TBounds, TVector> node, bool isParentChanged, bool isLastChild)
        {
            this.nodeIndex = nodeIndex;
            this.node = node;
            this.isParentChanged = isParentChanged;
            this.isLastChild = isLastChild;
        }
    }
}
