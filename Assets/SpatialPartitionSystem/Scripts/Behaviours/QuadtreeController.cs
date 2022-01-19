using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    [RequireComponent(typeof(TwoDimensionalSpatialGameObject))]
    public sealed class QuadtreeController : MonoBehaviour
    {
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [SerializeField, Range(1, 16)] private int maxDepth = 8;
        
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
