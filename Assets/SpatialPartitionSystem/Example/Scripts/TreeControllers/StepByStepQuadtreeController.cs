using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public sealed class StepByStepQuadtreeController : StepByStepTreeController<AABB2D, Vector2>
    {
        protected override Dimension Dimension => Dimension.Two;
    }
}
