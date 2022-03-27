using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SpatialPartitionSystem.Core.Series;

using Random = UnityEngine.Random;

namespace SpatialPartitionSystem.Example
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoundsAgent))]
    public sealed class ObjectSpawner : MonoBehaviour
    {
        [Tooltip("The number of objects created.")]
        [SerializeField, Range(1, 100000)] private int objectsCount = 10;

        [Space]
        [SerializeField] private bool creationWithDelay = false;
        [Tooltip("The delay between object creation.")]
        [SerializeField, Range(0f, 5f)] private float creationDelay = 2f;
        
        [Space]
        [Tooltip("The prefab with created object.")]
        [SerializeField] private BoundsAgent prefab;
        [Tooltip("The parent container for the created objects.")]
        [SerializeField] private Transform objectsContainer;
        
        [Space]
        [SerializeField] UnityEvent<BoundsAgent> onObjectCreated = new UnityEvent<BoundsAgent>();
        
        private readonly List<BoundsAgent> _objects = new List<BoundsAgent>(capacity: 100);
        private BoundsAgent _rootBounds;

        private void Start()
        {
            _rootBounds = GetComponent<BoundsAgent>();

            if (creationWithDelay)
            {
                StartCoroutine(SpawnObjectsCoroutine());
            }
            else
            {
                SpawnObjects();
            }
        }

        private IEnumerator SpawnObjectsCoroutine()
        {
            BoundsAgent instance;
            
            for (int i = 0; i < objectsCount; i++)
            {
                instance = CreateObject();
                
                instance.name = $"{i}";
                instance.gameObject.SetActive(false);
                
                yield return new WaitForSeconds(creationDelay);
                
                _objects.Add(instance);
                onObjectCreated.Invoke(instance);
                
                instance.gameObject.SetActive(true);
            }
        }

        private void SpawnObjects()
        {
            BoundsAgent instance;
            
            for (int i = 0; i < objectsCount; i++)
            {
                instance = CreateObject();
                
                instance.name = $"{i}";
                instance.gameObject.SetActive(false);
                
                _objects.Add(instance);
                onObjectCreated.Invoke(instance);
                
                instance.gameObject.SetActive(true);
            }
        }

        private BoundsAgent CreateObject()
        {
            Vector3 min = _rootBounds.Min;
            Vector3 max = _rootBounds.Max;
                
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);
            float randomZ = Random.Range(min.z, max.z);

            return Instantiate(
                prefab,
                transform.TransformPoint(new Vector3(randomX, randomY, randomZ)),
                prefab.transform.rotation,
                objectsContainer
            );
        }
    }
}
