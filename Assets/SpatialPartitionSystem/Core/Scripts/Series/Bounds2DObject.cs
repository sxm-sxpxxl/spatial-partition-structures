using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public class Bounds2DObject : MonoBehaviour
    {
        [SerializeField] private Color boundsColor = Color.white;
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        public Transform Transform => _transform;
        public AABB2D Bounds => new AABB2D(transform.localPosition + bounds.center, bounds.extents);

        private Transform _transform;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(transform.localPosition + bounds.center, bounds.size);
        }

        private void Awake()
        {
            _transform = transform;
        }
    }
}