#define SKIP

using System;
using System.Collections.Generic;
using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoundsAgent))]
    public abstract class SimpleTreeController<TBounds, TVector> : MonoBehaviour
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
#if SKIP
        private const int MAX_TREE_LEVEL = 3;

        [SerializeField, Range(0, MAX_TREE_LEVEL - 1)] private int treeLevel = 0;
        [SerializeField] private Rect levelTreeLabelRect = new Rect(0f, 0f, 100f, 20f);
        [SerializeField] private int fontSize = 14;
#endif
        [Tooltip("The maximum number of objects per leaf node.")]
        [SerializeField, Range(1, 16)] private int maxLeafObjects = 8;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 8)] private int maxDepth = 4;

        [Space]
        [SerializeField] private GenericBoundsAgent<TBounds, TVector> queryBoundsObj;

#if SKIP
        private BaseSkipTree<Transform, TBounds, TVector> _tree;
#else
        private BaseTree<Transform, TBounds, TVector> _tree;
#endif

        private readonly Dictionary<GenericBoundsAgent<TBounds, TVector>, int> _treeNodesMap = new Dictionary<GenericBoundsAgent<TBounds, TVector>, int>(capacity: 100);
        private IReadOnlyList<Transform> _queryObjects;

        private readonly List<GenericBoundsAgent<TBounds, TVector>> _objects = new List<GenericBoundsAgent<TBounds, TVector>>(capacity: 1000);
        private int _lastTreeLevel = 0;
        
        protected abstract Dimension Dimension { get; }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = fontSize;
            GUI.Label(levelTreeLabelRect, $"Tree Level: {treeLevel}");
        }

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

            if (_queryObjects == null)
            {
                return;
            }
            
            Gizmos.color = Color.black;
            for (int i = 0; i < _queryObjects.Count; i++)
            {
                Gizmos.DrawSphere(_queryObjects[i].position, 0.05f);
            }
        }
        
        private void OnValidate()
        {
#if SKIP
            if (treeLevel != _lastTreeLevel)
            {
                SetActiveObjects(_tree.GetObjectsByLevel(treeLevel));
                _lastTreeLevel = treeLevel;
            }
#endif
        }
        
        private void Awake()
        {
#if SKIP
            _tree = new BaseSkipTree<Transform, TBounds, TVector>(MAX_TREE_LEVEL, Dimension, GetComponent<GenericBoundsAgent<TBounds, TVector>>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
#else
            _tree = new BaseTree<Transform, TBounds, TVector>(Dimension, GetComponent<GenericBoundsAgent<TBounds, TVector>>().Bounds, maxLeafObjects, maxDepth, _objects.Capacity);
#endif
        }

        private void Update()
        {
#if SKIP
            _queryObjects = _tree.ApproximateQuery(queryBoundsObj.Bounds);
#else
            _queryObjects = _tree.Query(queryBoundsObj.Bounds);
#endif
        }

        public void AddObject(BoundsAgent obj)
        {
            var castedObj = obj as GenericBoundsAgent<TBounds, TVector>;
            
            if (_tree.TryAdd(obj.Transform, castedObj.Bounds, out int objectIndex) == false)
            {
                return;
            }

            _treeNodesMap.Add(castedObj, objectIndex);
            _objects.Add(castedObj);
        }

        public void UpdateObject(BoundsAgent obj)
        {
            var castedObj = obj as GenericBoundsAgent<TBounds, TVector>;
            int newObjectIndex = _tree.Update(_treeNodesMap[castedObj], castedObj.Bounds);
            _treeNodesMap[castedObj] = newObjectIndex;
        }

        public void CleanUp()
        {
            _tree.CleanUp();
        }

        public void RemoveObject(GenericBoundsAgent<TBounds, TVector> obj)
        {
            _tree.Remove(_treeNodesMap[obj]);
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
