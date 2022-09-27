using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    public sealed class CompressedQuadtree<TObject> : BaseCompressedTree<TObject, AABB2D, Vector2> where TObject : class
    {
        public CompressedQuadtree(AABB2D rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
            : base(Dimension.Two, rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity) { }
    }
}
