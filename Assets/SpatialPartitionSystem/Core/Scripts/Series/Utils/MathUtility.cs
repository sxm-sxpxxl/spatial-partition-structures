using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    internal static class MathUtility
    {
        private const float FloatingPointComparisonEpsilon = 0.001f;

        public static bool IsGreaterOrEqual(float a, float b) => a > b || IsApproximateEqual(a, b);

        public static bool IsLessOrEqual(float a, float b) => a < b || IsApproximateEqual(a, b);
        
        public static bool IsApproximateEqual(Vector2 a, Vector2 b, float epsilon = FloatingPointComparisonEpsilon)
        {
            return IsApproximateEqual(a.x, b.x, epsilon) && IsApproximateEqual(a.y, b.y, epsilon);
        }
        
        public static bool IsApproximateEqual(float a, float b, float epsilon = FloatingPointComparisonEpsilon)
        {
            return Mathf.Abs(a - b) < epsilon;
        }
    }
}
