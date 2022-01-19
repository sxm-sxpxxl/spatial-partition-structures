using System;
using SpatialPartitionSystem.Core;
using UnityEngine;

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
            
            _octree.DebugDraw(localOffset: transform.position);
        }

        private void Awake()
        {
            var rawBounds = GetComponent<ThreeDimensionalSpatialGameObject>().RawBounds;
            _octree = new Octree<ThreeDimensionalSpatialGameObject>(rawBounds, maxObjects, maxDepth);
        }

        public void AddObject(SpatialGameObject obj)
        {
            var actualObj = obj as ThreeDimensionalSpatialGameObject;
            _octree.TryAdd(actualObj);
        }

        public void UpdateObject(SpatialGameObject obj)
        {
            var actualObj = obj as ThreeDimensionalSpatialGameObject;
            _octree.Update(actualObj);
        }

        public void RemoveObject(SpatialGameObject obj)
        {
            var actualObj = obj as ThreeDimensionalSpatialGameObject;
            _octree.TryRemove(actualObj);
        }
    }
}
