using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace SpatialPartitionSystem.Core.Series
{
    [DisallowMultipleComponent, RequireComponent(typeof(Bounds2DObject))]
    public sealed class ObjectSpawner : MonoBehaviour
    {
        [Tooltip("The number of objects created.")]
        [SerializeField, Range(1, 3000)] private int objectsCount = 10;
        [Tooltip("The delay between object creation.")]
        [SerializeField, Range(0f, 5f)] private float creationDelay = 2f;
        
        [Space]
        [Tooltip("The prefab with created object.")]
        [SerializeField] private Bounds2DObject objectPrefab;
        [Tooltip("The parent container for the created objects.")]
        [SerializeField] private Transform objectsContainer;
        
        [Space]
        [SerializeField] UnityEvent<Bounds2DObject> onObjectCreated = new UnityEvent<Bounds2DObject>();
        
        private readonly List<Bounds2DObject> _objects = new List<Bounds2DObject>(capacity: 100);

        private void Start()
        {
            StartCoroutine(SpawnObjectsCoroutine());
        }

        private IEnumerator SpawnObjectsCoroutine()
        {
            Bounds2DObject rootSpatialObject = GetComponent<Bounds2DObject>();
            
            for (int i = 0; i < objectsCount; i++)
            {
                Vector3 min = rootSpatialObject.Bounds.Min;
                Vector3 max = rootSpatialObject.Bounds.Max;
                
                float randomX = Random.Range(min.x, max.x);
                float randomY = Random.Range(min.y, max.y);
                float randomZ = Random.Range(min.z, max.z);

                var instance = Instantiate(
                    objectPrefab,
                    transform.TransformPoint(new Vector3(randomX, randomY, randomZ)),
                    objectPrefab.transform.rotation,
                    objectsContainer
                );
                instance.name = $"{i}";
                instance.gameObject.SetActive(false);
                
                yield return new WaitForSeconds(creationDelay);
                
                _objects.Add(instance);
                onObjectCreated.Invoke(instance);
                
                instance.gameObject.SetActive(true);
            }
        }
    }
}
