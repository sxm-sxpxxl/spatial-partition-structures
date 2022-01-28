using UnityEngine;

namespace SpatialPartitionSystem.Core.OldSeries
{
    internal interface IDebugDrawer
    {
        void SetColor(Color color);
        void DrawWireCube(Vector3 center, Vector3 size);
    }
}
