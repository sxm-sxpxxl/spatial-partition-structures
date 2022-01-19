using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace SpatialPartitionSystem.Behaviours
{
    [RequireComponent(typeof(SpatialGameObject))]
    public sealed class ObjectMotioner : MonoBehaviour
    {
        [SerializeField] private bool isMotionUpdated = false;
        [SerializeField, Range(0.1f, 10f)] private float speed = 1f;

        [Space]
        [SerializeField] private UnityEvent<SpatialGameObject> onObjectUpdated = new UnityEvent<SpatialGameObject>();
        
        private SpatialGameObject rootSpatialObject;
        private List<MobileObject> _objects = new List<MobileObject>(capacity: 100);

        private Vector3 InitialVelocity
        {
            get
            {
                Vector3 direction = Random.onUnitSphere;

                if (rootSpatialObject is TwoDimensionalSpatialGameObject)
                {
                    direction.z = 0f;
                }
            
                return speed * direction;
            }
        }
        
        private class MobileObject
        {
            public Vector3 velocity;
            public SpatialGameObject target;
        }
        
        private void Awake()
        {
            rootSpatialObject = GetComponent<SpatialGameObject>();
        }

        private void Update()
        {
            if (isMotionUpdated == false)
            {
                return;
            }
            
            for (int i = 0; i < _objects.Count; i++)
            {
                UpdatePositionFor(i);
                onObjectUpdated.Invoke(_objects[i].target);
            }
        }
        
        public void AddMobileObject(SpatialGameObject obj)
        {
            _objects.Add(new MobileObject { target = obj, velocity = InitialVelocity });
        }

        private void UpdatePositionFor(int index)
        {
            var objTransform = _objects[index].target.transform;
            var objVelocity = _objects[index].velocity;

            objTransform.position += objVelocity * Time.deltaTime;
            UpdateVelocityFor(index);
        }
        
        private void UpdateVelocityFor(int index)
        {
            var objVelocity = _objects[index].velocity;
            var objTransform = _objects[index].target.transform;
            
            var resultObjPosition = objTransform.position;

            Vector3 min = rootSpatialObject.BoundsMin;
            Vector3 max = rootSpatialObject.BoundsMax;
            
            if (objTransform.position.x > max.x || objTransform.position.x < min.x)
            {
                resultObjPosition.x = Mathf.Clamp(objTransform.position.x, min.x, max.x);
                objVelocity.x = -objVelocity.x;
            }
            
            if (objTransform.position.y > max.y || objTransform.position.y < min.y)
            {
                resultObjPosition.y = Mathf.Clamp(objTransform.position.y, min.y, max.y);
                objVelocity.y = -objVelocity.y;
            }

            if (objTransform.position.z > max.z || objTransform.position.z < min.z)
            {
                resultObjPosition.z = Mathf.Clamp(objTransform.position.z, min.z, max.z);
                objVelocity.z = -objVelocity.z;
            }

            objTransform.position = resultObjPosition;
            _objects[index].velocity = speed * objVelocity.normalized;
        }
    }
}
