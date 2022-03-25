#define SKIP

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series.Trees
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

        #if SKIP
        private SkipQuadtree<Transform> _tree;
        #else
        private CompressedQuadtree<Transform> _tree;
        #endif

        private readonly Dictionary<Bounds2DObject, int> treeNodesMap = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private IReadOnlyList<Transform> queryObjects;

        private readonly List<Bounds2DObject> _objects = new List<Bounds2DObject>(capacity: 1000);
        private int lastTreeLevel = 0;
        
        private void OnDrawGizmos()
        {
            if (_tree == null || queryBoundsObj == null)
            {
                return;
            }
            
            #if SKIP
            _tree.DebugDrawTreeLevel(treeLevel, relativeTransform: transform);
            #else
            _tree.DebugDraw(relativeTransform: transform);
            #endif

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
                SetActiveObjects(_tree.GetObjectsByLevel(treeLevel));
                #endif
                lastTreeLevel = treeLevel;
            }
        }
        
        private void Awake()
        {
            #if SKIP
            _tree = new SkipQuadtree<Transform>(MAX_TREE_LEVEL, GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
            #else
            _tree = new CompressedQuadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
            #endif
        }

        private void Update()
        {
            #if SKIP
            queryObjects = _tree.ApproximateQuery(queryBoundsObj.Bounds);
            #else
            queryObjects = _tree.Query(queryBoundsObj.Bounds);
            #endif
        }

        public void AddObject(Bounds2DObject obj)
        {
            if (_tree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex) == false)
            {
                return;
            }

            treeNodesMap.Add(obj, objectIndex);
            _objects.Add(obj);
        }

        public void UpdateObject(Bounds2DObject obj)
        {
            int newObjectIndex = _tree.Update(treeNodesMap[obj], obj.Bounds);
            treeNodesMap[obj] = newObjectIndex;
        }

        public void CleanUp()
        {
            _tree.CleanUp();
        }

        public void RemoveObject(Bounds2DObject obj)
        {
            _tree.Remove(treeNodesMap[obj]);
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