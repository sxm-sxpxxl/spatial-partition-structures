using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public class QuadtreeController : MonoBehaviour
    {
        [Header("Quadtree Settings")]
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

        private readonly List<SpatialGameObject> _quadtreeStoredObjects = new List<SpatialGameObject>(capacity: 100);
        private Quadtree<SpatialGameObject> _quadtree;

        private Vector3[] _objVelocities;

        private void OnDrawGizmos()
        {
            Gizmos.color = queryBoundaryColor;
            Gizmos.DrawCube(queryBounds.center, queryBounds.size);

            if (_quadtree == null)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }
            
            var queryObjects = _quadtree.Query(queryBounds, _quadtreeStoredObjects.Count);
            
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
            _quadtree = new Quadtree<SpatialGameObject>(bounds, maxObjects, maxDepth);
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

                var instance = Instantiate(randomObjectPrefab, new Vector3(randomX, randomY), Quaternion.identity, randomObjectsContainer);
                instance.name = instance.name + $" ({i})";
                instance.gameObject.SetActive(false);
                
                Debug.Log($"Adding \'{instance.name}\' random object...");
                yield return new WaitForSeconds(delay);
                
                _quadtree.TryAdd(instance);
                _quadtreeStoredObjects.Add(instance);
                
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
                
                _quadtree.TryAdd(target);
                _quadtreeStoredObjects.Add(target);

                target.gameObject.SetActive(true);
                Debug.Log($"Object \'{target.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            // StartCoroutine(RemoveObjectsCoroutine());
        }

        private void Update()
        {
            _quadtree.DebugDraw(true);
            
            for (int i = 0; i < _quadtreeStoredObjects.Count; i++)
            {
                if (isMotionUpdated)
                {
                    UpdatePositionFor(i);
                }
                
                _quadtree.Update(_quadtreeStoredObjects[i]);
            }
        }

        private void UpdatePositionFor(int index)
        {
            var objTransform = _quadtreeStoredObjects[index].transform;
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

            objTransform.position = objPosition;
            _objVelocities[index] = speed * objVelocity.normalized;
        }

        private void InitObjVelocities(int count)
        {
            _objVelocities = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                var dir = Random.onUnitSphere;
                dir.z = 0f;

                _objVelocities[i] = speed * dir;
            }
        }

        private IEnumerator RemoveObjectsCoroutine()
        {
            for (int i = 0; i < _quadtreeStoredObjects.Count; i++)
            {
                var removedObject = _quadtreeStoredObjects[i];
                Debug.Log($"Removing \'{removedObject.name}\' object...");
                yield return new WaitForSeconds(delay);
                
                _quadtree.TryRemove(removedObject);
                
                removedObject.gameObject.SetActive(false);
                Debug.Log($"Object \'{removedObject.name}\' was removed!");
                Debug.Log($"************************************");
            }
            
            _quadtreeStoredObjects.Clear();
        }

        private void ClearQuadtree()
        {
            foreach (var removedObject in _quadtreeStoredObjects)
            {
                removedObject.gameObject.SetActive(false);
            }

            _quadtree.Clear();
        }
    }
}
