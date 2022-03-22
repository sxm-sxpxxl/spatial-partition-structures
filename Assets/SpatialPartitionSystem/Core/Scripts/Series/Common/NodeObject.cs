namespace SpatialPartitionSystem.Core.Series
{
    internal struct NodeObject<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public int leafIndex;
        public TObject target;
        public TBounds bounds;
    }
}
