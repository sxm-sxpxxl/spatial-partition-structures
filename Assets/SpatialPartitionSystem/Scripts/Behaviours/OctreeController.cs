using System;
using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    [RequireComponent(typeof(ThreeDimensionalSpatialGameObject))]
    public sealed class OctreeController : MonoBehaviour
    {
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [SerializeField, Range(1, 16)] private int maxDepth = 8;
        
        private Octree<ThreeDimensionalSpatialGameObject> _octree;

        private void OnDrawGizmos()
        {
            if (_octree == null)
            {
                return;
            }
            
            _octree.DebugDraw(relativeTransform: transform);
        }

        private void Awake()
        {
            var rawBounds = GetComponent<ThreeDimensionalSpatialGameObject>().Bounds;
            _octree = new Octree<ThreeDimensionalSpatialGameObject>(rawBounds, maxObjects, maxDepth);
        }

        public void AddObject(SpatialGameObject obj)
        {
            _octree.TryAdd(obj as ThreeDimensionalSpatialGameObject);
        }

        public void UpdateObject(SpatialGameObject obj)
        {
            _octree.Update(obj as ThreeDimensionalSpatialGameObject);
        }

        public void RemoveObject(SpatialGameObject obj)
        {
            _octree.TryRemove(obj as ThreeDimensionalSpatialGameObject);
        }
    }
}
