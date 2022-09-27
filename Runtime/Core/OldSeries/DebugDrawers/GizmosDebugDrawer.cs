using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.OldSeries
{
    internal sealed class GizmosDebugDrawer : IDebugDrawer
    {
        public void SetColor(Color color)
        {
            Gizmos.color = color;
        }

        public void DrawWireCube(Vector3 center, Vector3 size)
        {
            Gizmos.DrawWireCube(center, size);
        }
    }
}
