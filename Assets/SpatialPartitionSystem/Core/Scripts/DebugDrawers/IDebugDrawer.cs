using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    internal interface IDebugDrawer
    {
        void SetColor(Color color);
        void DrawWireCube(Vector3 center, Vector3 size);
    }
}
