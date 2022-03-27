using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public class Bounds3DAgent : GenericBoundsAgent<AABB3D, Vector3>
    {
        public override AABB3D Bounds => new AABB3D(AdjustedCenter + bounds.center, bounds.extents);
        
        protected override Vector3 Center => Bounds.TransformCenter(Transform) - AdjustedCenter;
        protected override Vector3 Size => Bounds.TransformSize(Transform);
    }
}
