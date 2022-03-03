using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace SpatialPartitionSystem.Core.Series
{
    [DisallowMultipleComponent, RequireComponent(typeof(Bounds2DObject))]
    public sealed class ObjectMotioner : MonoBehaviour
    {
        [Tooltip("Do you need to update the motion of the objects?")]
        [SerializeField] private bool isMotionUpdated = false;
        [Tooltip("The speed of the object motion (unit per second).")]
        [SerializeField, Range(0.1f, 10f)] private float speed = 1f;

        [Space]
        [SerializeField] private UnityEvent<Bounds2DObject> onObjectUpdated = new UnityEvent<Bounds2DObject>();
        [SerializeField] private UnityEvent onAllObjectsUpdated = new UnityEvent();
        
        private Bounds2DObject _rootSpatialObject;
        private readonly List<MobileObject> _objects = new List<MobileObject>(capacity: 100);

        private Vector3 InitialVelocity
        {
            get
            {
                Vector3 direction = Random.onUnitSphere;

                if (_rootSpatialObject is Bounds2DObject)
                {
                    direction.z = 0f;
                }
            
                return speed * direction;
            }
        }
        
        private class MobileObject
        {
            public Vector3 velocity;
            public Bounds2DObject target;
        }
        
        private void Awake()
        {
            _rootSpatialObject = GetComponent<Bounds2DObject>();
        }

        private void Update()
        {
            if (isMotionUpdated == false)
            {
                return;
            }
            
            for (int i = 0; i < _objects.Count; i++)
            {
                UpdatePositionFor(_objects[i]);
                onObjectUpdated.Invoke(_objects[i].target);
            }
            
            onAllObjectsUpdated.Invoke();
        }
        
        public void AddMobileObject(Bounds2DObject obj)
        {
            _objects.Add(new MobileObject { target = obj, velocity = InitialVelocity });
        }

        private void UpdatePositionFor(MobileObject obj)
        {
            var objTransform = obj.target.transform;
            var objPosition = objTransform.localPosition;
            
            var resultObjVelocity = obj.velocity;

            Vector3 min = _rootSpatialObject.Bounds.Min;
            Vector3 max = _rootSpatialObject.Bounds.Max;
            
            if (objPosition.x > max.x || objPosition.x < min.x)
            {
                objPosition.x = Mathf.Clamp(objPosition.x, min.x, max.x);
                resultObjVelocity.x = -resultObjVelocity.x;
            }
            
            if (objPosition.y > max.y  || objPosition.y < min.y)
            {
                objPosition.y = Mathf.Clamp(objPosition.y, min.y, max.y);
                resultObjVelocity.y = -resultObjVelocity.y;
            }

            // if (_rootSpatialObject is ThreeDimensionalSpatialGameObject)
            // {
            //     if (objPosition.z > max.z || objPosition.z < min.z)
            //     {
            //         objPosition.z = Mathf.Clamp(objPosition.z, min.z, max.z);
            //         resultObjVelocity.z = -resultObjVelocity.z;
            //     }                
            // }

            resultObjVelocity = speed * resultObjVelocity.normalized;
            objPosition += resultObjVelocity * Time.deltaTime;

            obj.velocity = resultObjVelocity;
            obj.target.transform.localPosition = objPosition;
        }
    }
}
