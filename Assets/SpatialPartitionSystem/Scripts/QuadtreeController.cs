using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialPartitionSystem
{
    public class QuadtreeController : MonoBehaviour
    {
        [Header("Quadtree parameters")]
        [SerializeField] private Bounds bounds = new Bounds();
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [SerializeField, Range(1, 16)] private int maxDepth = 8;

        [Space]
        [SerializeField] private Bounds queryBoundary = new Bounds();
        [SerializeField] private Color queryBoundaryColor = Color.green;
        
        [Space]
        [SerializeField] private SpatialGameObject[] storedObjects;
        [SerializeField] private bool isRandomObjectsAdded = false;
        [SerializeField, Range(1, 50)] private int randomObjectsCount = 10;
        
        [Space]
        [SerializeField] private SpatialGameObject randomObjectPrefab;
        [SerializeField] private Transform randomObjectsContainer;

        [Space]
        [SerializeField, Range(0f, 5f)] private float delay = 2f;

        private List<SpatialGameObject> quadtreeStoredObjects = new List<SpatialGameObject>(capacity: 100);
        private Quadtree<SpatialGameObject> quadtree;

        private void OnDrawGizmos()
        {
            Gizmos.color = queryBoundaryColor;
            Gizmos.DrawCube(queryBoundary.center, queryBoundary.size);

            if (quadtree == null)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }

            var queryObjects = quadtree.Query(queryBoundary);
            
            if (queryObjects != null)
            {
                Gizmos.color = Color.white;
                foreach (var obj in queryObjects)
                {
                    Gizmos.DrawSphere(obj.transform.position, 0.1f);
                }
            }
        }

        private void Awake()
        {
            quadtree = new Quadtree<SpatialGameObject>(bounds, maxObjects, maxDepth);
            HideAllObjects();
        }

        private void Start()
        {
            if (isRandomObjectsAdded)
            {
                StartCoroutine(AddRandomObjectsCoroutine());
            }
            else
            {
                StartCoroutine(AddStoredObjectsCoroutine());   
            }
        }
        
        private void HideAllObjects()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                storedObjects[i].gameObject.SetActive(false);
            }
        }

        private IEnumerator AddRandomObjectsCoroutine()
        {
            for (int i = 0; i < randomObjectsCount; i++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);

                var instance = Instantiate(randomObjectPrefab, new Vector3(randomX, randomY), Quaternion.identity, randomObjectsContainer);
                instance.name = instance.name + $" ({i})";
                instance.gameObject.SetActive(false);
                
                Debug.Log($"Adding \'{instance.name}\' random object...");
                yield return new WaitForSeconds(delay);
                
                quadtree.TryAdd(instance);
                quadtreeStoredObjects.Add(instance);
                
                instance.gameObject.SetActive(true);
                
                Debug.Log($"Object \'{instance.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            StartCoroutine(RemoveObjectsCoroutine());
        }
        
        private IEnumerator AddStoredObjectsCoroutine()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                var target = storedObjects[i];
                Debug.Log($"Adding \'{target.name}\' stored object...");
                yield return new WaitForSeconds(delay);
                
                quadtree.TryAdd(target);
                quadtreeStoredObjects.Add(target);


                target.gameObject.SetActive(true);
                Debug.Log($"Object \'{target.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            // StartCoroutine(RemoveObjectsCoroutine());
        }

        private void Update()
        {
            quadtree.DebugDraw(true);
        }

        private IEnumerator RemoveObjectsCoroutine()
        {
            for (int i = 0; i < quadtreeStoredObjects.Count; i++)
            {
                var removedObject = quadtreeStoredObjects[i];
                Debug.Log($"Removing \'{removedObject.name}\' object...");
                yield return new WaitForSeconds(delay);
                
                quadtree.TryRemove(removedObject);
                
                removedObject.gameObject.SetActive(false);
                Debug.Log($"Object \'{removedObject.name}\' was removed!");
                Debug.Log($"************************************");
            }

            quadtreeStoredObjects.Clear();
        }
    }
}
