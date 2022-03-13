using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    [Serializable]
    public struct AABB2D : IEquatable<AABB2D>
    {
        private const float EPSILON = 0.001f;
        
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

        public bool Equals(AABB2D other) => IsApproximateEqual(center, other.center) && IsApproximateEqual(extents, other.extents);
        
        public override bool Equals(object obj) => obj is AABB2D other && Equals(other);
        
        public override int GetHashCode() => unchecked (center.GetHashCode() * 397) ^ extents.GetHashCode();
        
        public static bool operator ==(AABB2D a, AABB2D b) => a.Equals(b);
        
        public static bool operator !=(AABB2D a, AABB2D b) => !a.Equals(b);

        private static bool IsGreaterOrEqual(float a, float b) => a > b || IsApproximateEqual(a, b);

        private static bool IsLessOrEqual(float a, float b) => a < b || IsApproximateEqual(a, b);

        private static bool IsApproximateEqual(float a, float b, float epsilon = EPSILON) => a == b || Mathf.Abs(a - b) < epsilon;

        private static bool IsApproximateEqual(Vector2 a, Vector2 b, float epsilon = EPSILON) => IsApproximateEqual(a.x, b.x, epsilon) && IsApproximateEqual(a.y, b.y, epsilon);
    }
}
