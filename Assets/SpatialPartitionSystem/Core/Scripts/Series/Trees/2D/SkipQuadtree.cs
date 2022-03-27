using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public sealed class SkipQuadtree<TObject> : BaseSkipTree<TObject, AABB2D, Vector2> where TObject : class
    {
        public SkipQuadtree(int levelsQuantity, AABB2D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
            : base(levelsQuantity, Dimension.Two, bounds, maxLeafObjects, maxDepth, initialObjectsCapacity) { }
    }
}
