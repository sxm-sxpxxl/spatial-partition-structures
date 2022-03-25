using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public struct AABB3D : IAABB<Vector3>
    {
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 extents;

        public Vector3 Center => center;
        
        public Vector3 Extents => extents;

        public Vector3 Size => 2f * extents;

        public Vector3 Min => center - extents;

        public Vector3 Max => center + extents;

        public AABB3D(Vector3 center, Vector3 extents)
        {
            this.center = center;
            this.extents = extents;
        }

        public bool Contains(Vector3 point) => Contains(point, Min, Max);

        public bool Contains<TBounds>(TBounds other) where TBounds : IAABB<Vector3>
        {
            Vector3 min = Min, max = Max, otherCenter = other.Center, otherExtents = other.Extents;
            return Contains(otherCenter + new Vector3(-otherExtents.x, -otherExtents.y, otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(otherExtents.x, otherExtents.y, otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(otherExtents.x, -otherExtents.y, otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(-otherExtents.x, otherExtents.y, otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(-otherExtents.x, -otherExtents.y, -otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(otherExtents.x, otherExtents.y, -otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(otherExtents.x, -otherExtents.y, -otherExtents.z), min, max) &&
                   Contains(otherCenter + new Vector3(-otherExtents.x, otherExtents.y, -otherExtents.z), min, max);
        }

        public bool Intersects<TBounds>(TBounds other) where TBounds : IAABB<Vector3>
        {
            Vector3 otherCenter = other.Center, otherExtents = other.Extents;
            return MathUtility.IsLessOrEqual(Mathf.Abs(center.x - otherCenter.x), extents.x + otherExtents.x) &&
                   MathUtility.IsLessOrEqual(Mathf.Abs(center.y - otherCenter.y), extents.y + otherExtents.y) &&
                   MathUtility.IsLessOrEqual(Mathf.Abs(center.z - otherCenter.z), extents.z + otherExtents.z);
        }

        public float IntersectionArea<TBounds>(TBounds other) where TBounds : IAABB<Vector3>
        {
            Vector3 otherCenter = other.Center, otherExtents = other.Extents;
            float areaWidth = (extents.x + otherExtents.x) - Mathf.Abs(center.x - otherCenter.x);
            float areaHeight = (extents.y + otherExtents.y) - Mathf.Abs(center.y - otherCenter.y);
            float areaDepth = (extents.z + otherExtents.z) - Mathf.Abs(center.z - otherCenter.z);
            
            return areaWidth < 0f || areaHeight < 0f || areaDepth < 0f ? 0f : areaWidth * areaHeight * areaDepth;
        }
        
        public IAABB<Vector3> GetChildBoundsBy(SplitSection splitSectionIndex)
        {
            Vector3 childExtents = 0.5f * extents;
            Vector3 childCenter = Vector3.zero;
            
            switch (splitSectionIndex)
            {
                case SplitSection.One:
                    childCenter = center + new Vector3(childExtents.x, childExtents.y, childExtents.z);
                    break;
                case SplitSection.Two:
                    childCenter = center + new Vector3(-childExtents.x, childExtents.y, childExtents.z);
                    break;
                case SplitSection.Three:
                    childCenter = center + new Vector3(-childExtents.x, -childExtents.y, childExtents.z);
                    break;
                case SplitSection.Four:
                    childCenter = center + new Vector3(childExtents.x, -childExtents.y, childExtents.z);
                    break;
                case SplitSection.Five:
                    childCenter = center + new Vector3(childExtents.x, childExtents.y, -childExtents.z);
                    break;
                case SplitSection.Six:
                    childCenter = center + new Vector3(-childExtents.x, childExtents.y, -childExtents.z);
                    break;
                case SplitSection.Seven:
                    childCenter = center + new Vector3(-childExtents.x, -childExtents.y, -childExtents.z);
                    break;
                case SplitSection.Eight:
                    childCenter = center + new Vector3(childExtents.x, -childExtents.y, -childExtents.z);
                    break;
            }

            return new AABB3D(childCenter, childExtents);
        }
        
        public IAABB<Vector3> GetExtendedBoundsOn(float offset)
        {
            Assert.IsTrue(offset > 0);
            return new AABB3D(center, extents + offset * Vector3.one);
        }
        
        public Vector3 TransformCenter(Transform relativeTransform) => relativeTransform.TransformPoint(center);

        public Vector3 TransformSize(Transform relativeTransform) => relativeTransform.TransformDirection(Size);

        public bool Equals<TBounds>(TBounds other) where TBounds : IAABB<Vector3>
        {
            return MathUtility.IsApproximateEqual(center, other.Center) && MathUtility.IsApproximateEqual(extents, other.Extents);
        }

        public override bool Equals(object obj) => obj is AABB3D other && Equals(other);
        
        public override int GetHashCode() => unchecked (center.GetHashCode() * 397) ^ extents.GetHashCode();
        
        private static bool Contains(Vector3 point, Vector3 min, Vector3 max)
        {
            return MathUtility.IsGreaterOrEqual(point.x, min.x) && MathUtility.IsLessOrEqual(point.x, max.x) &&
                   MathUtility.IsGreaterOrEqual(point.y, min.y) && MathUtility.IsLessOrEqual(point.y, max.y) &&
                   MathUtility.IsGreaterOrEqual(point.z, min.z) && MathUtility.IsLessOrEqual(point.z, max.z);
        }
    }
}