#define SKIP

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

        // private Quadtree<Transform> _quadtree;
        #if SKIP
        private SkipQuadtree<Transform> _quadtree;
        #else
        private CompressedQuadtree<Transform> _quadtree;
        #endif

        private readonly Dictionary<Bounds2DObject, int> treeNodesMap = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private IReadOnlyList<Transform> queryObjects;

        private readonly List<Bounds2DObject> _objects = new List<Bounds2DObject>(capacity: 1000);
        private int lastTreeLevel = 0;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null || queryBoundsObj == null)
            {
                return;
            }
            
            #if SKIP
            _quadtree.DebugDraw(treeLevel, relativeTransform: transform);
            #else
            _quadtree.DebugDraw(relativeTransform: transform);
            #endif

            // queryObjects = _quadtree.Query(queryBoundsObj.Bounds);
            // queryObjects = _quadtree.ApproximateQuery(queryBoundsObj.Bounds);
            
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
                #if SKIP
                SetActiveObjects(_quadtree.GetObjectsByLevel(treeLevel));
                #endif
                lastTreeLevel = treeLevel;
            }
        }
        
        private void Awake()
        {
            // _quadtree = new Quadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
            #if SKIP
            _quadtree = new SkipQuadtree<Transform>(MAX_TREE_LEVEL, GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
            #else
            _quadtree = new CompressedQuadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
            #endif
        }

        private void Update()
        {
            #if SKIP
            queryObjects = _quadtree.ApproximateQuery(queryBoundsObj.Bounds);
            #else
            queryObjects = _quadtree.Query(queryBoundsObj.Bounds);
            #endif
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
            // int newObjectIndex = _quadtree.Update(treeNodesMap[obj], obj.Transform, obj.Bounds);
            int newObjectIndex = _quadtree.Update(treeNodesMap[obj], obj.Bounds);
            treeNodesMap[obj] = newObjectIndex;
            
            if (queryBoundsObj == null)
            {
                return;
            }
            
            // queryObjects = _quadtree.ApproximateQuery(queryBoundsObj.Bounds);
        }

        public void CleanUp()
        {
            _quadtree.CleanUp();
            // queryObjects = _quadtree.ApproximateQuery(queryBoundsObj.Bounds);
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