using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public sealed class SpatialGameObject : MonoBehaviour, ISpatialObject
    {
        public Bounds Bounds => new Bounds(transform.position + bounds.center, bounds.size);

        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        [SerializeField] private Color boundsColor = Color.green;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);
        }
    }
}
