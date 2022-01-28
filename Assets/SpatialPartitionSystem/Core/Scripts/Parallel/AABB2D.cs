using System;
using Unity.Mathematics;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Parallel
{
    [Serializable]
    public struct AABB2D
    {
        [SerializeField] private float2 center;
        [SerializeField] private float2 extents;

        public float2 Center => center;
        
        public float2 Extents => extents;

        public float2 Size => 2f * extents;

        public float2 Min => center - extents;

        public float2 Max => center + extents;

        public AABB2D(float2 center, float2 extents)
        {
            this.center = center;
            this.extents = extents;
        }

        public bool Contains(float2 point)
        {
            return point.x > Min.x && point.x < Max.x
                && point.y > Min.y && point.y < Max.y;
        }

        public bool Contains(AABB2D other)
        {
            return Contains(other.center + new float2(-other.extents.x, -other.extents.y)) &&
                   Contains(other.center + new float2(-other.extents.x, other.extents.y)) &&
                   Contains(other.center + new float2(other.extents.x, -other.extents.y)) &&
                   Contains(other.center + new float2(other.extents.x, other.extents.y));
        }

        public bool Intersects(AABB2D other)
        {
            return math.abs(center.x - other.center.x) < extents.x + other.extents.x &&
                   math.abs(center.y - other.center.y) < extents.y + other.extents.y;
        }
    }
}
