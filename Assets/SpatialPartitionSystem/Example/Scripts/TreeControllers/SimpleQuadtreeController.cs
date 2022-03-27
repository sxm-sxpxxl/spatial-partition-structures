using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public sealed class SimpleQuadtreeController : SimpleTreeController<AABB2D, Vector2>
    {
        protected override Dimension Dimension => Dimension.Two;
    }
}
