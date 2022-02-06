using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    [DisallowMultipleComponent, RequireComponent(typeof(Bounds2DObject))]
    public sealed class OptimizedQuadtreeController : MonoBehaviour
    {
        [Tooltip("The maximum number of objects per leaf node.")]
        [SerializeField, Range(1, 16)] private sbyte maxLeafObjects = 8;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 8)] private sbyte maxDepth = 4;
        
        private Quadtree<Transform> _quadtree;

        private readonly Dictionary<Bounds2DObject, int> map = new Dictionary<Bounds2DObject, int>(capacity: 100);

        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }
            
            _quadtree.DebugDraw(relativeTransform: transform);
        }
        
        private void Awake()
        {
            _quadtree = new Quadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, 100);
        }

        public void AddObject(Bounds2DObject obj)
        {
            _quadtree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex);
            map.Add(obj, objectIndex);
        }

        public void UpdateObject(Bounds2DObject obj)
        {
            _quadtree.TryUpdate(obj.Transform, obj.Bounds, map[obj], out int newObjectIndex);
            map[obj] = newObjectIndex;
        }

        public void CleanUp()
        {
            _quadtree.CleanUp();
        }

        public void RemoveObject(Bounds2DObject obj)
        {
            _quadtree.TryRemove(map[obj]);
        }
    }
}