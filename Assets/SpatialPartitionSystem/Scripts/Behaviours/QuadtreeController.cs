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
        
        [Space]
        [SerializeField] private PlaneOrientation planeOrientation = PlaneOrientation.XY;

        private Quadtree<TwoDimensionalSpatialGameObject> _quadtree;

        private void Awake()
        {
            var rawBounds = GetComponent<TwoDimensionalSpatialGameObject>().RawBounds;
            _quadtree = new Quadtree<TwoDimensionalSpatialGameObject>(rawBounds, maxObjects, maxDepth, planeOrientation);
        }

        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }
            
            _quadtree.DebugDraw(localOffset: transform.position);
        }

        public void AddObject(SpatialGameObject obj)
        {
            var actualObj = obj as TwoDimensionalSpatialGameObject;
            _quadtree.TryAdd(actualObj);
        }

        public void UpdateObject(SpatialGameObject obj)
        {
            var actualObj = obj as TwoDimensionalSpatialGameObject;
            _quadtree.Update(actualObj);
        }

        public void RemoveObject(SpatialGameObject obj)
        {
            var actualObj = obj as TwoDimensionalSpatialGameObject;
            _quadtree.TryRemove(actualObj);
        }
    }
}
