using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Example
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpatialGameObject))]
    public sealed class ObjectSpawner : MonoBehaviour
    {
        [Tooltip("The number of objects created.")]
        [SerializeField, Range(1, 100)] private int objectsCount = 10;
        [Tooltip("The delay between object creation.")]
        [SerializeField, Range(0f, 5f)] private float creationDelay = 2f;
        
        [Space]
        [Tooltip("The prefab with created object.")]
        [SerializeField] private SpatialGameObject objectPrefab;
        [Tooltip("The parent container for the created objects.")]
        [SerializeField] private Transform objectsContainer;
        
        [Space]
        [SerializeField] UnityEvent<SpatialGameObject> onObjectCreated = new UnityEvent<SpatialGameObject>();
        
        private readonly List<SpatialGameObject> _objects = new List<SpatialGameObject>(capacity: 100);

        private void Start()
        {
            StartCoroutine(SpawnObjectsCoroutine());
        }

        private IEnumerator SpawnObjectsCoroutine()
        {
            SpatialGameObject rootSpatialObject = GetComponent<SpatialGameObject>();
            
            for (int i = 0; i < objectsCount; i++)
            {
                Vector3 min = rootSpatialObject.BoundsMin;
                Vector3 max = rootSpatialObject.BoundsMax;
                
                float randomX = Random.Range(min.x, max.x);
                float randomY = Random.Range(min.y, max.y);
                float randomZ = Random.Range(min.z, max.z);

                var instance = Instantiate(
                    objectPrefab,
                    transform.TransformPoint(new Vector3(randomX, randomY, randomZ)),
                    objectPrefab.transform.rotation,
                    objectsContainer
                );
                instance.name = instance.name + $" ({i})";
                instance.gameObject.SetActive(false);
                
                yield return new WaitForSeconds(creationDelay);
                
                _objects.Add(instance);
                onObjectCreated.Invoke(instance);
                
                instance.gameObject.SetActive(true);
            }
        }
    }
}
