using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialPartitionSystem
{
    public class OctreeController : MonoBehaviour
    {
        [Header("Octree Settings")]
        [SerializeField] private Bounds bounds = new Bounds();
        [SerializeField, Range(1, 8)] private int maxObjects = 4;
        [SerializeField, Range(1, 16)] private int maxDepth = 8;

        [Space]
        [SerializeField] private Bounds queryBounds = new Bounds();
        [SerializeField] private Color queryBoundaryColor = Color.green;
        
        [Header("Spawn Object Settings")]
        [SerializeField] private SpatialGameObject[] storedObjects;
        [SerializeField] private bool isRandomObjectsAdded = false;
        [SerializeField, Range(1, 100)] private int randomObjectsCount = 10;
        
        [Space]
        [SerializeField] private SpatialGameObject randomObjectPrefab;
        [SerializeField] private Transform randomObjectsContainer;

        [Space]
        [SerializeField, Range(0f, 5f)] private float delay = 2f;

        [Header("Motion Object Settings")]
        [SerializeField] private bool isMotionUpdated = false;
        [SerializeField, Range(0.1f, 10f)] private float speed = 1f;

        private readonly List<SpatialGameObject> _octreeStoredObjects = new List<SpatialGameObject>(capacity: 100);
        private Octree<SpatialGameObject> _octree;

        private Vector3[] _objVelocities;

        private void OnDrawGizmos()
        {
            Gizmos.color = queryBoundaryColor;
            Gizmos.DrawCube(queryBounds.center, queryBounds.size);

            if (_octree == null)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }
            
            var queryObjects = _octree.Query(queryBounds, _octreeStoredObjects.Count);
            
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
            _octree = new Octree<SpatialGameObject>(bounds, maxObjects, maxDepth);
            HideAllObjects();
        }

        private void Start()
        {
            StartCoroutine(isRandomObjectsAdded ? AddRandomObjectsCoroutine() : AddStoredObjectsCoroutine());
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
            InitObjVelocities(randomObjectsCount);
            
            for (int i = 0; i < randomObjectsCount; i++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                float randomZ = Random.Range(bounds.min.z, bounds.max.z);

                var instance = Instantiate(randomObjectPrefab, new Vector3(randomX, randomY, randomZ), Quaternion.identity, randomObjectsContainer);
                instance.name = instance.name + $" ({i})";
                instance.gameObject.SetActive(false);
                
                Debug.Log($"Adding \'{instance.name}\' random object...");
                yield return new WaitForSeconds(delay);
                
                _octree.TryAdd(instance);
                _octreeStoredObjects.Add(instance);
                
                instance.gameObject.SetActive(true);
                
                Debug.Log($"Object \'{instance.name}\' was added!");
                Debug.Log($"************************************");
            }

            // ClearQuadtree();
            // StartCoroutine(RemoveObjectsCoroutine());
        }
        
        private IEnumerator AddStoredObjectsCoroutine()
        {
            InitObjVelocities(storedObjects.Length);
            
            for (int i = 0; i < storedObjects.Length; i++)
            {
                var target = storedObjects[i];
                Debug.Log($"Adding \'{target.name}\' stored object...");
                yield return new WaitForSeconds(delay);
                
                _octree.TryAdd(target);
                _octreeStoredObjects.Add(target);

                target.gameObject.SetActive(true);
                Debug.Log($"Object \'{target.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            // StartCoroutine(RemoveObjectsCoroutine());
        }

        private void Update()
        {
            _octree.DebugDraw(true);
            
            for (int i = 0; i < _octreeStoredObjects.Count; i++)
            {
                if (isMotionUpdated)
                {
                    UpdatePositionFor(i);
                }
                
                _octree.Update(_octreeStoredObjects[i]);
            }
        }

        private void UpdatePositionFor(int index)
        {
            var objTransform = _octreeStoredObjects[index].transform;
            var objVelocity = _objVelocities[index];

            objTransform.position += objVelocity * Time.deltaTime;
            UpdateVelocityFor(objTransform, index);
        }

        private void UpdateVelocityFor(Transform objTransform, int index)
        {
            var objVelocity = _objVelocities[index];
            var objPosition = objTransform.position;
            
            if (objTransform.position.x > bounds.max.x || objTransform.position.x < bounds.min.x)
            {
                objPosition.x = Mathf.Clamp(objTransform.position.x, bounds.min.x, bounds.max.x);
                objVelocity.x = -objVelocity.x;
            }
            
            if (objTransform.position.y > bounds.max.y || objTransform.position.y < bounds.min.y)
            {
                objPosition.y = Mathf.Clamp(objTransform.position.y, bounds.min.y, bounds.max.y);
                objVelocity.y = -objVelocity.y;
            }
            
            if (objTransform.position.z > bounds.max.z || objTransform.position.z < bounds.min.z)
            {
                objPosition.z = Mathf.Clamp(objTransform.position.z, bounds.min.z, bounds.max.z);
                objVelocity.z = -objVelocity.z;
            }

            objTransform.position = objPosition;
            _objVelocities[index] = speed * objVelocity.normalized;
        }

        private void InitObjVelocities(int count)
        {
            _objVelocities = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                _objVelocities[i] = speed * Random.onUnitSphere;
            }
        }

        private IEnumerator RemoveObjectsCoroutine()
        {
            for (int i = 0; i < _octreeStoredObjects.Count; i++)
            {
                var removedObject = _octreeStoredObjects[i];
                Debug.Log($"Removing \'{removedObject.name}\' object...");
                yield return new WaitForSeconds(delay);
                
                _octree.TryRemove(removedObject);
                
                removedObject.gameObject.SetActive(false);
                Debug.Log($"Object \'{removedObject.name}\' was removed!");
                Debug.Log($"************************************");
            }
            
            _octreeStoredObjects.Clear();
        }

        private void ClearQuadtree()
        {
            foreach (var removedObject in _octreeStoredObjects)
            {
                removedObject.gameObject.SetActive(false);
            }

            _octree.Clear();
        }
    }
}
