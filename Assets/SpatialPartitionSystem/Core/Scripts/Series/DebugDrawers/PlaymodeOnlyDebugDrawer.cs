using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    internal class PlaymodeOnlyDebugDrawer : IDebugDrawer
    {
        private Color _color = Color.white;
        
        public void SetColor(Color color)
        {
            _color = color;
        }

        public void DrawWireCube(Vector3 center, Vector3 size)
        {
            Vector3 localForwardOffset = 0.5f * size.z * Vector3.forward;
            Vector3 localUpOffset = 0.5f * size.y * Vector3.up;
            Vector3 localRightUpOffset = 0.5f * size.x * Vector3.right;
            
            DrawWirePlane(center + localUpOffset, Vector3.forward, Vector3.right, size.z, size.x);
            DrawWirePlane(center + localRightUpOffset, Vector3.up, Vector3.forward, size.y, size.z);
            DrawWirePlane(center + localForwardOffset, Vector3.up, Vector3.right, size.y, size.x);
            
            DrawWirePlane(center - localUpOffset, Vector3.forward, Vector3.right, size.z, size.x);
            DrawWirePlane(center - localRightUpOffset, Vector3.up, Vector3.forward, size.y, size.z);
            DrawWirePlane(center - localForwardOffset, Vector3.up, Vector3.right, size.y, size.x);
        }

        private void DrawWirePlane(Vector3 center, Vector3 up, Vector3 right, float height, float width)
        {
            Vector3 upOffset = 0.5f * height * up;
            Vector3 rightOffset = 0.5f * width * right;

            Vector3 point1 = center - upOffset - rightOffset;
            Vector3 point2 = center + upOffset - rightOffset;
            Vector3 point3 = center + upOffset + rightOffset;
            Vector3 point4 = center - upOffset + rightOffset;
            
            Debug.DrawLine(point1, point2, _color);
            Debug.DrawLine(point2, point3, _color);
            Debug.DrawLine(point3, point4, _color);
            Debug.DrawLine(point4, point1, _color);
        }
    }
}
