using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace SpatialPartitionSystem.Core.Parallel
{
    public unsafe struct NativeQuadtree<TObject> : IDisposable where TObject : unmanaged
    {
        private const int MAX_POSSIBLE_DEPTH = 8;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle _safetyHandle;
        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel _disposeSentinel;
#endif
        [NativeDisableUnsafePtrRestriction] private UnsafeList* _objects;
        [NativeDisableUnsafePtrRestriction] private UnsafeList* _nodes;

        private int _objectsCount;
        private int _maxDepth;
        private short _maxLeafObjects;

        private AABB2D _rootBounds;

        public NativeQuadtree(AABB2D bounds, int maxDepth = 8, short maxLeafObjects = 4, int initialObjectsCapacity = 256,
            Allocator allocator = Allocator.Temp)
        {
            _rootBounds = bounds;
            _maxDepth = math.clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);
            _maxLeafObjects = maxLeafObjects;
            _objectsCount = 0;

            int maxNodesCount = (int) math.pow(4, maxDepth);

            _nodes = UnsafeList.Create(
                UnsafeUtility.SizeOf<Node>(),
                UnsafeUtility.AlignOf<Node>(),
                maxNodesCount,
                allocator,
                NativeArrayOptions.ClearMemory
            );
            
            _objects = UnsafeList.Create(
                UnsafeUtility.SizeOf<NodeObject<TObject>>(),
                UnsafeUtility.AlignOf<NodeObject<TObject>>(),
                initialObjectsCapacity,
                allocator,
                NativeArrayOptions.ClearMemory
            );
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            UnsafeUtility.IsUnmanaged<TObject>();
            DisposeSentinel.Create(out _safetyHandle, out _disposeSentinel, 1, allocator);
#endif
        }
        
        

        public void Dispose()
        {
            UnsafeList.Destroy(_objects);
            _objects = null;

            UnsafeList.Destroy(_nodes);
            _nodes = null;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref _safetyHandle, ref _disposeSentinel);
#endif
        }
    }
}