using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    [Serializable]
    public struct AABB2D : IAABB<Vector2>
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

        public bool Contains(Vector2 point) => Contains(point, Min, Max);

        public bool Contains(IAABB<Vector2> other)
        {
            Vector2 min = Min, max = Max, otherCenter = other.Center, otherExtents = other.Extents;
            return Contains(otherCenter + new Vector2(-otherExtents.x, -otherExtents.y), min, max) &&
                   Contains(otherCenter + new Vector2(otherExtents.x, otherExtents.y), min, max) &&
                   Contains(otherCenter + new Vector2(otherExtents.x, -otherExtents.y), min, max) &&
                   Contains(otherCenter + new Vector2(-otherExtents.x, otherExtents.y), min, max);
        }

        public bool Intersects(IAABB<Vector2> other)
        {
            Vector2 otherCenter = other.Center, otherExtents = other.Extents;
            return MathUtility.IsLessOrEqual(Mathf.Abs(center.x - otherCenter.x), extents.x + otherExtents.x) &&
                   MathUtility.IsLessOrEqual(Mathf.Abs(center.y - otherCenter.y), extents.y + otherExtents.y);
        }

        public float IntersectionArea(IAABB<Vector2> other)
        {
            Vector2 otherCenter = other.Center, otherExtents = other.Extents;
            float areaWidth = (extents.x + otherExtents.x) - Mathf.Abs(center.x - otherCenter.x);
            float areaHeight = (extents.y + otherExtents.y) - Mathf.Abs(center.y - otherCenter.y);
            
            return areaWidth < 0f || areaHeight < 0f ? 0f : areaWidth * areaHeight;
        }

        public IAABB<Vector2> GetChildBoundsBy(SplitSection splitSectionIndex)
        {
            Vector2 childExtents = 0.5f * extents;
            Vector2 childCenter = Vector2.zero;
            
            switch (splitSectionIndex)
            {
                case SplitSection.One:
                    childCenter = center + new Vector2(childExtents.x, childExtents.y);
                    break;
                case SplitSection.Two:
                    childCenter = center + new Vector2(-childExtents.x, childExtents.y);
                    break;
                case SplitSection.Three:
                    childCenter = center + new Vector2(-childExtents.x, -childExtents.y);
                    break;
                case SplitSection.Four:
                    childCenter = center + new Vector2(childExtents.x, -childExtents.y);
                    break;
            }

            return new AABB2D(childCenter, childExtents);
        }

        public IAABB<Vector2> GetExtendedBoundsOn(float offset)
        {
            Assert.IsTrue(offset > 0);
            return new AABB2D(center, extents + offset * Vector2.one);
        }

        public Vector3 TransformCenter(Transform relativeTransform) => relativeTransform.TransformPoint(center);

        public Vector3 TransformSize(Transform relativeTransform) => relativeTransform.TransformDirection(Size);

        public bool Equals(IAABB<Vector2> other)
        {
            return MathUtility.IsApproximateEqual(center, other.Center) && MathUtility.IsApproximateEqual(extents, other.Extents);
        }

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
