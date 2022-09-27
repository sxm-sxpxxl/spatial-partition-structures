using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    public sealed class Quadtree<TObject> : BaseTree<TObject, AABB2D, Vector2> where TObject : class
    {
        public Quadtree(AABB2D rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
            : base(Dimension.Two, rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity) { }
    }
}
