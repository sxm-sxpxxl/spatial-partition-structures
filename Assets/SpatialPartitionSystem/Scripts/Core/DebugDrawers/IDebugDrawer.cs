using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public interface IDebugDrawer
    {
        void SetColor(Color color);
        void DrawWireCube(Vector3 center, Vector3 size);
    }
}
