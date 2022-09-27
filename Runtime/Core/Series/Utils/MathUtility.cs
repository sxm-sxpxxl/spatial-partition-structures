using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal static class MathUtility
    {
        public static bool IsGreaterOrEqual(float a, float b) => a > b || IsApproximateEqual(a, b);

        public static bool IsLessOrEqual(float a, float b) => a < b || IsApproximateEqual(a, b);

        public static bool IsApproximateEqual(Vector3 a, Vector3 b)
        {
            return IsApproximateEqual(a.x, b.x) && IsApproximateEqual(a.y, b.y) && IsApproximateEqual(a.z, b.z);
        }
        
        public static bool IsApproximateEqual(Vector2 a, Vector2 b)
        {
            return IsApproximateEqual(a.x, b.x) && IsApproximateEqual(a.y, b.y);
        }
        
        public static bool IsApproximateEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < Mathf.Epsilon;
        }
    }
}
