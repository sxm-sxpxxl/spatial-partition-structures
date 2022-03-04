using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public class StepByStepController : MonoBehaviour
    {
        [SerializeField] private Bounds2DObject[] objects;
        
        [Header("Options")]
        [SerializeField] private bool stepByStepMode = true;
        [SerializeField] private KeyCode addObjectKey = KeyCode.A;
        [SerializeField] private KeyCode removeObjectKey = KeyCode.D;
        [SerializeField] private KeyCode cleanUpKey = KeyCode.C;
        [SerializeField] private Bounds2DObject objectForRemove;

        [Header("Debug Info")]
        [SerializeField] private int currentObjectIndex = 0;

        private readonly Dictionary<Bounds2DObject, int> objectTreeIndexes = new Dictionary<Bounds2DObject, int>(capacity: 100);
        private CompressedQuadtree<Transform> _quadtree;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }

            _quadtree.DebugDraw(transform);
        }

        private void Start()
        {
            _quadtree = new CompressedQuadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, 1, 8, 8);

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

        private void AddObjectToTree(Bounds2DObject obj)
        {
            _quadtree.TryAdd(obj.Transform, obj.Bounds, out int objectIndex);
            objectTreeIndexes.Add(obj, objectIndex);
            currentObjectIndex++;
            
            obj.gameObject.SetActive(true);
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
