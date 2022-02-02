using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public class Bounds2DObject : MonoBehaviour
    {
        [SerializeField] private Color boundsColor = Color.white;
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        
        public AABB2D Bounds => new AABB2D(transform.localPosition + bounds.center, bounds.extents);

        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(transform.localPosition + bounds.center, bounds.size);
        }
    }
}