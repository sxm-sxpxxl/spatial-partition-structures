using System;
using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Example
{
    [DisallowMultipleComponent, RequireComponent(typeof(ThreeDimensionalSpatialGameObject))]
    public sealed class OctreeController : MonoBehaviour
    {
        [Tooltip("The maximum number of objects per node.")]
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 16)] private int maxDepth = 8;
        
        [Space]
        [Tooltip("The spatial object with its bounds to request an intersection with tree objects.")]
        [SerializeField] private ThreeDimensionalSpatialGameObject querySpatialObject;
        [Tooltip("The bounds color of the queried objects for debug visualization.")]
        [SerializeField] private Color queryObjectBoundsColor = Color.white;
        
        private Octree<ThreeDimensionalSpatialGameObject> _octree;

        private void OnDrawGizmos()
        {
            if (_octree == null)
            {
                return;
            }
            
            _octree.DebugDraw(relativeTransform: transform);
            
            if (querySpatialObject == null)
            {
                return;
            }

            var queryObjects = _octree.Query(querySpatialObject.LocalBounds, 100);
            foreach (var obj in queryObjects)
            {
                Gizmos.color = queryObjectBoundsColor;
                Gizmos.DrawCube(obj.WorldBoundsCenter, obj.WorldBoundsSize);
            }
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
