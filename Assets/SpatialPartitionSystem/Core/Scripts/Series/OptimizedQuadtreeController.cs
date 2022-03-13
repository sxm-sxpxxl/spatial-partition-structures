using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    [DisallowMultipleComponent, RequireComponent(typeof(Bounds2DObject))]
    public sealed class OptimizedQuadtreeController : MonoBehaviour
    {
        private const int MAX_TREE_LEVEL = 3;
        
        [SerializeField, Range(0, MAX_TREE_LEVEL - 1)] private int treeLevel = 0;
        
        [Space]
        [Tooltip("The maximum number of objects per leaf node.")]
        [SerializeField, Range(1, 16)] private sbyte maxLeafObjects = 8;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 8)] private sbyte maxDepth = 4;

        [Space]
        [SerializeField] private Bounds2DObject queryBoundsObj;
        
        private SkipQuadtree<Transform> _quadtree;
        private readonly Dictionary<Bounds2DObject, int> treeNodesMap = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private IReadOnlyList<Transform> queryObjects;

        private readonly List<Bounds2DObject> _objects = new List<Bounds2DObject>(capacity: 1000);
        private int lastTreeLevel = 0;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }
            
            _quadtree.DebugDraw(treeLevel, relativeTransform: transform);
            // _quadtree.DebugDraw(relativeTransform: transform);

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
        
        private void OnValidate()
        {
            if (treeLevel != lastTreeLevel)
            {
                SetActiveObjects(_quadtree.GetObjectsByLevel(treeLevel));
                lastTreeLevel = treeLevel;
            }
        }
        
        private void Awake()
        {
            _quadtree = new SkipQuadtree<Transform>(MAX_TREE_LEVEL, GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, 100);
            // _quadtree = new CompressedQuadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, 100);
        }

        public void AddObject(Bounds2DObject obj)
        {
            if (_quadtree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex) == false)
            {
                return;
            }

            treeNodesMap.Add(obj, objectIndex);
            _objects.Add(obj);
        }

        public void UpdateObject(Bounds2DObject obj)
        {
            int newObjectIndex = _quadtree.Update(treeNodesMap[obj], obj.Bounds);
            treeNodesMap[obj] = newObjectIndex;
            
            // if (queryBoundsObj == null)
            // {
            //     return;
            // }
            //
            // queryObjects = _quadtree.Query(queryBoundsObj.Bounds);
        }

        public void CleanUp()
        {
            _quadtree.CleanUp();
        }

        public void RemoveObject(Bounds2DObject obj)
        {
            _quadtree.TryRemove(treeNodesMap[obj]);
            _objects.Remove(obj);
        }
        
        private void SetActiveObjects(IReadOnlyList<Transform> activeObjects)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                _objects[i].gameObject.SetActive(false);
            }
            
            for (int i = 0; i < activeObjects.Count; i++)
            {
                activeObjects[i].gameObject.SetActive(true);
            }
        }
    }
}