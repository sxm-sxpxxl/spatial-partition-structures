using UnityEngine;

namespace SpatialPartitionSystem.Core.Series.Trees
{
    public sealed class CompressedOctree<TObject> : BaseCompressedTree<TObject, AABB3D, Vector3> where TObject : class
    {
        public CompressedOctree(AABB3D rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
            : base(Dimension.Three, rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity) { }
    }
}
