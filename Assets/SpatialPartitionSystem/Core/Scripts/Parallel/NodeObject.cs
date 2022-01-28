using Unity.Mathematics;

namespace SpatialPartitionSystem.Core.Parallel
{
    public struct NodeObject<T> where T : unmanaged
    {
        public float2 position;
        public T target;
    }
}
