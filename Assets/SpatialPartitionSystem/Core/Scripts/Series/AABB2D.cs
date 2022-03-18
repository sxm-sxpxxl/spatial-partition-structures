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

        public bool Contains(Vector2 point) => Contains(point, Min, Max);

        public bool Contains(AABB2D other)
        {
            Vector2 min = Min, max = Max;
            return Contains(other.center + new Vector2(-other.extents.x, -other.extents.y), min, max) &&
                   Contains(other.center + new Vector2(other.extents.x, other.extents.y), min, max) &&
                   Contains(other.center + new Vector2(other.extents.x, -other.extents.y), min, max) &&
                   Contains(other.center + new Vector2(-other.extents.x, other.extents.y), min, max);
        }

        public bool Intersects(AABB2D other)
        {
            return MathUtility.IsLessOrEqual(Mathf.Abs(center.x - other.center.x), extents.x + other.extents.x) &&
                   MathUtility.IsLessOrEqual(Mathf.Abs(center.y - other.center.y), extents.y + other.extents.y);
        }

        public float IntersectionArea(AABB2D other)
        {
            float areaWidth = (extents.x + other.extents.x) - Mathf.Abs(center.x - other.center.x);
            float areaHeight = (extents.y + other.extents.y) - Mathf.Abs(center.y - other.center.y);
            
            return areaWidth < 0 || areaHeight < 0 ? 0f : areaWidth * areaHeight;
        }

        public bool Equals(AABB2D other) => MathUtility.IsApproximateEqual(center, other.center) && MathUtility.IsApproximateEqual(extents, other.extents);
        
        public override bool Equals(object obj) => obj is AABB2D other && Equals(other);
        
        public override int GetHashCode() => unchecked (center.GetHashCode() * 397) ^ extents.GetHashCode();
        
        public static bool operator ==(AABB2D a, AABB2D b) => a.Equals(b);
        
        public static bool operator !=(AABB2D a, AABB2D b) => !a.Equals(b);

        private static bool Contains(Vector2 point, Vector2 min, Vector2 max)
        {
            return MathUtility.IsGreaterOrEqual(point.x, min.x) && MathUtility.IsLessOrEqual(point.x, max.x) &&
                   MathUtility.IsGreaterOrEqual(point.y, min.y) && MathUtility.IsLessOrEqual(point.y, max.y);
        }
    }
}
