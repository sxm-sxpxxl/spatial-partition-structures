namespace SpatialPartitionSystem.Core.Series
{
    internal struct NodeObject<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private const int Null = SpatialTree<TObject, TBounds, TVector>.Null;
        
        public int leafIndex;
        public int nextObjectIndex;
        public bool isMissing;
        public TObject target;
        public TBounds bounds;

        public NodeObject(int leafIndex, TObject target, TBounds bounds)
        {
            this.leafIndex = leafIndex;
            nextObjectIndex = Null;
            isMissing = false;
            this.target = target;
            this.bounds = bounds;
        }
    }
}
