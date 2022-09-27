using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Sample
{
    [DisallowMultipleComponent]
    public abstract class BoundsAgent : MonoBehaviour
    {
        [SerializeField] private bool isInnerBounds = false;
        [SerializeField] private Color boundsColor = Color.white;
        [SerializeField] protected Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        
        private Transform _transform;
        
        public Transform Transform => _transform == null ? transform : _transform;

        protected Vector3 AdjustedCenter => isInnerBounds ? Transform.localPosition : Vector3.zero;
        protected abstract Vector3 Center { get; }
        protected abstract Vector3 Size { get; }

        public Vector3 Min => bounds.min;
        public Vector3 Max => bounds.max;

        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawCube(Center, Size);
        }

        private void Awake()
        {
            _transform = transform;
        }
    }
}
