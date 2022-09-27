using Unity.Mathematics;

namespace Sxm.SpatialPartitionStructures.Core.Parallel
{
    public struct NodeObject<T> where T : unmanaged
    {
        public float2 position;
        public T target;
    }
}
