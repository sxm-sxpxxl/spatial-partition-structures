#define SKIP

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoundsAgent))]
    public abstract class StepByStepTreeController<TBounds, TVector> : MonoBehaviour
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
#if SKIP
        private const int MAX_TREE_LEVEL = 3;
#endif
        [Tooltip("The maximum number of objects per leaf node.")]
        [SerializeField, Range(1, 16)] private int maxLeafObjects = 8;
        [Tooltip("The maximum depth of the tree. Non-fitting objects are placed in already created nodes.")]
        [SerializeField, Range(1, 8)] private int maxDepth = 4;
        
        [Space]
        [SerializeField] private GenericBoundsAgent<TBounds, TVector>[] objects;

        [Space]
        [SerializeField] private GenericBoundsAgent<TBounds, TVector> queryBoundsObj;
        
        [Header("Options")]
        [SerializeField] private bool stepByStepMode = true;
        [SerializeField] private KeyCode addObjectKey = KeyCode.A;
        [SerializeField] private KeyCode removeObjectKey = KeyCode.D;
        [SerializeField] private KeyCode cleanUpKey = KeyCode.C;
        [SerializeField] private GenericBoundsAgent<TBounds, TVector> forRemove;

        [Header("Debug Info")]
        [SerializeField] private int currentObjectIndex = 0;
#if SKIP
        [SerializeField, Range(0, MAX_TREE_LEVEL - 1)] private int treeLevel = 0;
#endif
        
        [Space]
        [SerializeField] UnityEvent<GenericBoundsAgent<TBounds, TVector>> onObjectCreated = new UnityEvent<GenericBoundsAgent<TBounds, TVector>>();

        private readonly Dictionary<GenericBoundsAgent<TBounds, TVector>, int> _treeNodesMap = new Dictionary<GenericBoundsAgent<TBounds, TVector>, int>(capacity: 100);
        private readonly Dictionary<GenericBoundsAgent<TBounds, TVector>, int> _objectTreeIndexes = new Dictionary<GenericBoundsAgent<TBounds, TVector>, int>(capacity: 100);
        
#if SKIP
        private BaseSkipTree<Transform, TBounds, TVector> _tree;
#else
        private BaseTree<Transform, TBounds, TVector> _tree;
#endif
        private IReadOnlyList<Transform> _queryObjects;

#if SKIP
        private int _lastTreeLevel = 0;
#endif
        protected abstract Dimension Dimension { get; }

        private void OnDrawGizmos()
        {
            if (_tree == null || queryBoundsObj == null)
            {
                return;
            }

#if SKIP
            _tree.DebugDrawTreeLevel(treeLevel, transform);
#else
            _tree.DebugDraw(transform);
#endif
            
#if SKIP
            _queryObjects = _tree.ApproximateQuery(queryBoundsObj.Bounds);
#else
            _queryObjects = _tree.Query(queryBoundsObj.Bounds);
#endif

            if (_queryObjects == null)
            {
                return;
            }
            
            Gizmos.color = Color.black;
            for (int i = 0; i < _queryObjects.Count; i++)
            {
                Gizmos.DrawSphere(_queryObjects[i].position, 0.1f);
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

        private void Start()
        {
#if SKIP
            _tree = new BaseSkipTree<Transform, TBounds, TVector>(MAX_TREE_LEVEL, Dimension, GetComponent<GenericBoundsAgent<TBounds, TVector>>().Bounds, maxLeafObjects, maxDepth, objects.Length);
#else
            _tree = new BaseTree<Transform, TBounds, TVector>(Dimension, GetComponent<GenericBoundsAgent<TBounds, TVector>>().Bounds, maxLeafObjects, maxDepth, objects.Length);
#endif

            DisableAllObjects();
            
            if (stepByStepMode)
            {
                Debug.Log($"*<color=yellow>{addObjectKey}</color> key - add object to quadtree " +
                          $"| *<color=yellow>{removeObjectKey}</color> key - remove selected object from quadtree " +
                          $"| *<color=yellow>{cleanUpKey}</color> key - clean up the quadtree.");
                return;
            }
            
            for (int i = 0; i < objects.Length; i++)
            {
                AddObjectToTree(objects[i]);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(addObjectKey) && currentObjectIndex < objects.Length)
            {
                AddObjectToTree(objects[currentObjectIndex]);
            }

            if (Input.GetKeyDown(removeObjectKey))
            {
                if (forRemove == null)
                {
                    Debug.Log($"<color=yellow>\'{nameof(forRemove)}\'</color> is <color=red>NULL</color>!");
                    return;
                }

                if (_objectTreeIndexes.TryGetValue(forRemove, out int objectIndex) == false)
                {
                    Debug.Log($"<color=yellow>\'{nameof(forRemove)}\'</color> wasn't <color=green>ADDED</color> to quadtree!");
                    return;
                }

                _tree.Remove(objectIndex);
                
                forRemove.gameObject.SetActive(false);
                Debug.Log($"Object <color=yellow>\'{forRemove.name}\'</color> was <color=red>REMOVED</color> to quadtree!");
                forRemove = null;
            }

            if (Input.GetKeyDown(cleanUpKey))
            {
                _tree.CleanUp();
                Debug.Log($"<color=yellow>Quadtree</color> was <color=green>CLEAN UP</color>!");
            }
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

        private void SetActiveObjects(IReadOnlyList<Transform> activeObjects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].gameObject.SetActive(false);
            }
            
            for (int i = 0; i < activeObjects.Count; i++)
            {
                activeObjects[i].gameObject.SetActive(true);
            }
        }

        private void AddObjectToTree(GenericBoundsAgent<TBounds, TVector> obj)
        {
            _tree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex);
            _objectTreeIndexes.Add(obj, objectIndex);
            currentObjectIndex++;
            
            obj.gameObject.SetActive(true);

            _treeNodesMap.Add(obj, objectIndex);
            onObjectCreated.Invoke(obj);
            
            Debug.Log($"Object <color=yellow>\'{obj.name}\'</color> was <color=green>ADDED</color> to quadtree!");
        }

        private void DisableAllObjects()
        {
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].gameObject.SetActive(false);
            }
        }
    }
}
