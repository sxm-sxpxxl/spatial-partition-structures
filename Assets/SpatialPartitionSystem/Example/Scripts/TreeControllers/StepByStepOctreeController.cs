using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public sealed class StepByStepOctreeController : StepByStepTreeController<AABB3D, Vector3>
    {
        protected override Dimension Dimension => Dimension.Three;
    }
}
