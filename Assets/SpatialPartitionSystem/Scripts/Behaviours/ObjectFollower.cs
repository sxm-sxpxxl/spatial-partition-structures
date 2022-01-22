using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        SceneView.RepaintAll();
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
