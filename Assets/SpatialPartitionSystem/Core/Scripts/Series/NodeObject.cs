namespace SpatialPartitionSystem.Core.Series
{
    internal struct NodeObject<TObject> where TObject : class
    {
        public TObject target;
        public AABB2D bounds;
    }
}
