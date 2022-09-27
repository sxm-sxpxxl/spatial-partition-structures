using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sxm.SpatialPartitionStructures.Sample
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class ObjectFollower : MonoBehaviour
    {
        public bool IsFollowing { get; private set; }

        [SerializeField] private Transform selectedObject;

        public void StartFollowing()
        {
            if (selectedObject == null)
            {
                Debug.LogWarning("Object wasn't selected!");
                return;
            }

            IsFollowing = true;
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }

        public void StopFollowing()
        {
            IsFollowing = false;
        }

        private void Update()
        {
            if (selectedObject == null || IsFollowing == false)
            {
                return;
            }

            transform.position = selectedObject.position;
        }
    }
}
