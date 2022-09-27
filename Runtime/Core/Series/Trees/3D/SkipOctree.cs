using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    public sealed class SkipOctree<TObject> : BaseSkipTree<TObject, AABB3D, Vector3> where TObject : class
    {
        public SkipOctree(int levelsQuantity, AABB3D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
            : base(levelsQuantity, Dimension.Three, bounds, maxLeafObjects, maxDepth, initialObjectsCapacity) { }
    }
}
