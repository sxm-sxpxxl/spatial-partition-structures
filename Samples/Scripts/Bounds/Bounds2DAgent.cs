using UnityEngine;
using Sxm.SpatialPartitionStructures.Core.Series;

namespace Sxm.SpatialPartitionStructures.Sample
{
    public class Bounds2DAgent : GenericBoundsAgent<AABB2D, Vector2>
    {
        public override AABB2D Bounds => new AABB2D(AdjustedCenter + bounds.center, bounds.extents);

        protected override Vector3 Center => Bounds.TransformCenter(Transform) - AdjustedCenter;
        protected override Vector3 Size => Bounds.TransformSize(Transform);
    }
}
