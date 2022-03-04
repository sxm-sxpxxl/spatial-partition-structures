using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    [Serializable]
    public struct AABB2D
    {
        [SerializeField] private Vector2 center;
        [SerializeField] private Vector2 extents;

        public Vector2 Center => center;
        
        public Vector2 Extents => extents;

        public Vector2 Size => 2f * extents;

        public Vector2 Min => center - extents;

        public Vector2 Max => center + extents;

        public AABB2D(Vector2 center, Vector2 extents)
        {
            this.center = center;
            this.extents = extents;
        }

        public bool Contains(Vector2 point)
        {
            return IsGreaterOrEqual(point.x, Min.x) && IsLessOrEqual(point.x, Max.x)
                && IsGreaterOrEqual(point.y, Min.y) && IsLessOrEqual(point.y, Max.y);
        }

        public bool Contains(AABB2D other)
        {
            return Contains(other.center + new Vector2(-other.extents.x, -other.extents.y)) &&
                   Contains(other.center + new Vector2(-other.extents.x, other.extents.y)) &&
                   Contains(other.center + new Vector2(other.extents.x, -other.extents.y)) &&
                   Contains(other.center + new Vector2(other.extents.x, other.extents.y));
        }

        public bool Intersects(AABB2D other)
        {
            return IsLessOrEqual(Mathf.Abs(center.x - other.center.x), extents.x + other.extents.x) &&
                   IsLessOrEqual(Mathf.Abs(center.y - other.center.y), extents.y + other.extents.y);
        }

        private bool IsGreaterOrEqual(float a, float b) => a > b || Mathf.Approximately(a, b);
        
        private bool IsLessOrEqual(float a, float b) => a < b || Mathf.Approximately(a, b);
    }
}
