using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialPartitionSystem.Core.Series
{
    public class StepByStepController : MonoBehaviour
    {
        private const int MAX_TREE_LEVEL = 3;
        
        [SerializeField] private Bounds2DObject[] objects;
        
        [Header("Options")]
        [SerializeField] private bool stepByStepMode = true;
        [SerializeField] private KeyCode addObjectKey = KeyCode.A;
        [SerializeField] private KeyCode removeObjectKey = KeyCode.D;
        [SerializeField] private KeyCode cleanUpKey = KeyCode.C;
        [SerializeField] private Bounds2DObject objectForRemove;

        [Header("Debug Info")]
        [SerializeField] private int currentObjectIndex = 0;
        [SerializeField, Range(0, MAX_TREE_LEVEL - 1)] private int treeLevel = 0;
        
        [Space]
        [SerializeField] UnityEvent<Bounds2DObject> onObjectCreated = new UnityEvent<Bounds2DObject>();

        private readonly Dictionary<Bounds2DObject, int> treeNodesMap = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private readonly Dictionary<Bounds2DObject, int> objectTreeIndexes = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private SkipQuadtree<Transform> _quadtree;

        private int lastTreeLevel = 0;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }

            _quadtree.DebugDraw(treeLevel, transform);
            // _quadtree.DebugDraw(transform);
        }

        private void OnValidate()
        {
            if (treeLevel != lastTreeLevel)
            {
                SetActiveObjects(_quadtree.GetObjectsByLevel(treeLevel));
                lastTreeLevel = treeLevel;
            }
        }

        private void Start()
        {
            _quadtree = new SkipQuadtree<Transform>(3, GetComponent<Bounds2DObject>().Bounds, 1, 5, 8);

            DisabledAllObjects();
            
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
                if (objectForRemove == null)
                {
                    Debug.Log($"<color=yellow>\'{nameof(objectForRemove)}\'</color> is <color=red>NULL</color>!");
                    return;
                }

                if (objectTreeIndexes.TryGetValue(objectForRemove, out int objectIndex) == false)
                {
                    Debug.Log($"<color=yellow>\'{nameof(objectForRemove)}\'</color> wasn't <color=green>ADDED</color> to quadtree!");
                    return;
                }

                if (_quadtree.TryRemove(objectIndex))
                {
                    objectForRemove.gameObject.SetActive(false);
                    Debug.Log($"Object <color=yellow>\'{objectForRemove.name}\'</color> was <color=red>REMOVED</color> to quadtree!");
                    objectForRemove = null;
                }
            }

            if (Input.GetKeyDown(cleanUpKey))
            {
                _quadtree.CleanUp();
                Debug.Log($"<color=yellow>Quadtree</color> was <color=green>CLEAN UP</color>!");
            }
        }

        public void UpdateObject(Bounds2DObject obj)
        {
            int newObjectIndex = _quadtree.Update(treeNodesMap[obj], obj.Transform, obj.Bounds);
            treeNodesMap[obj] = newObjectIndex;
        }

        public void CleanUp()
        {
            _quadtree.CleanUp();
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

        private void AddObjectToTree(Bounds2DObject obj)
        {
            _quadtree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex);
            objectTreeIndexes.Add(obj, objectIndex);
            currentObjectIndex++;
            
            obj.gameObject.SetActive(true);

            treeNodesMap.Add(obj, objectIndex);
            onObjectCreated.Invoke(obj);
            
            Debug.Log($"Object <color=yellow>\'{obj.name}\'</color> was <color=green>ADDED</color> to quadtree!");
        }

        private void DisabledAllObjects()
        {
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].gameObject.SetActive(false);
            }
        }
    }
}
