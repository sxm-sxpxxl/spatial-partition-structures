using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectFollower : MonoBehaviour
{
    [SerializeField] private Transform selectedObject;

    private bool _isFollowing = false;
    
    [ContextMenu("Follow to selected object")]
    public void FollowToSelectedObject()
    {
        if (selectedObject == null)
        {
            return;
        }

        _isFollowing = true;
    }

    [ContextMenu("Unfollow selected object")]
    public void UnFollow()
    {
        _isFollowing = false;
    }

    private void Update()
    {
        if (selectedObject == null || _isFollowing == false)
        {
            return;
        }

        transform.position = selectedObject.position;
    }
}
