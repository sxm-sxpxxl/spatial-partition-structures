using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    [DisallowMultipleComponent, RequireComponent(typeof(TwoDimensionalSpatialGameObject))]
    public sealed class QuadtreeController : MonoBehaviour
    {
        [Tooltip("The maximum number of objects per node.")]
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 16)] private int maxDepth = 8;

        [Space]
        [Tooltip("The spatial object with its bounds to request an intersection with tree objects.")]
        [SerializeField] private TwoDimensionalSpatialGameObject querySpatialObject;
        [Tooltip("The bounds color of the queried objects for debug visualization.")]
        [SerializeField] private Color queryObjectBoundsColor = Color.white;
        
        private Quadtree<TwoDimensionalSpatialGameObject> _quadtree;
        private TwoDimensionalSpatialGameObject _rootSpatialObject;

        private void Awake()
        {
            _rootSpatialObject = GetComponent<TwoDimensionalSpatialGameObject>();
            _quadtree = new Quadtree<TwoDimensionalSpatialGameObject>(_rootSpatialObject.Bounds, maxObjects, maxDepth);
        }

        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }
            
            _quadtree.DebugDraw(relativeTransform: transform);

            if (querySpatialObject == null)
            {
                return;
            }

            var queryObjects = _quadtree.Query(querySpatialObject.LocalBounds, 100);
            foreach (var obj in queryObjects)
            {
                Gizmos.color = queryObjectBoundsColor;
                Gizmos.DrawCube(obj.WorldBoundsCenter, obj.WorldBoundsSize);
            }
        }

        public void AddObject(SpatialGameObject obj)
        {
            _quadtree.TryAdd(obj as TwoDimensionalSpatialGameObject);
        }

        public void UpdateObject(SpatialGameObject obj)
        {
            _quadtree.Update(obj as TwoDimensionalSpatialGameObject);
        }

        public void RemoveObject(SpatialGameObject obj)
        {
            _quadtree.TryRemove(obj as TwoDimensionalSpatialGameObject);
        }
    }
}
