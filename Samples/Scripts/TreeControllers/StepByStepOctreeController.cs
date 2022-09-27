using UnityEngine;
using Sxm.SpatialPartitionStructures.Core.Series;

namespace Sxm.SpatialPartitionStructures.Sample
{
    public sealed class StepByStepOctreeController : StepByStepTreeController<AABB3D, Vector3>
    {
        protected override Dimension Dimension => Dimension.Three;
    }
}
