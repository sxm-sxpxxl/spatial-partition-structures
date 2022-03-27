using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public sealed class SimpleOctreeController : SimpleTreeController<AABB3D, Vector3>
    {
        protected override Dimension Dimension => Dimension.Three;
    }
}
