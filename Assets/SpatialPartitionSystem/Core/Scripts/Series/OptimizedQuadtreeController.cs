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

        [Space]
        [SerializeField] private Bounds2DObject queryBoundsObj;
        
        private Quadtree<Transform> _quadtree;
        private readonly Dictionary<Bounds2DObject, int> treeNodesMap = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private IReadOnlyList<Transform> queryObjects;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }
            
            _quadtree.DebugDraw(relativeTransform: transform);

            if (queryObjects == null)
            {
                return;
            }
            
            Gizmos.color = Color.black;
            for (int i = 0; i < queryObjects.Count; i++)
            {
                Gizmos.DrawSphere(queryObjects[i].position, 0.025f);
            }
        }
        
        private void Awake()
        {
            _quadtree = new Quadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, 100);
        }

        public void AddObject(Bounds2DObject obj)
        {
            if (_quadtree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex) == false)
            {
                return;
            }
            
            treeNodesMap.Add(obj, objectIndex);
        }

        public void UpdateObject(Bounds2DObject obj)
        {
            int newObjectIndex = _quadtree.Update(treeNodesMap[obj], obj.Transform, obj.Bounds);
            treeNodesMap[obj] = newObjectIndex;
            
            // if (queryBoundsObj == null)
            // {
            //     return;
            // }

            // queryObjects = _quadtree.Query(queryBoundsObj.Bounds);
        }

        public void CleanUp()
        {
            _quadtree.CleanUp();
        }

        public void RemoveObject(Bounds2DObject obj)
        {
            _quadtree.TryRemove(treeNodesMap[obj]);
        }
    }
}