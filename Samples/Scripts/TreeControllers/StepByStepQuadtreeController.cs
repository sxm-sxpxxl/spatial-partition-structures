using UnityEngine;
using Sxm.SpatialPartitionStructures.Core.Series;

namespace Sxm.SpatialPartitionStructures.Sample
{
    public sealed class StepByStepQuadtreeController : StepByStepTreeController<AABB2D, Vector2>
    {
        protected override Dimension Dimension => Dimension.Two;
    }
}
