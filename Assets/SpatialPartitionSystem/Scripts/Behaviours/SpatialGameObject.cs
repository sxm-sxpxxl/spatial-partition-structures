using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public abstract class SpatialGameObject<TBounds> : SpatialGameObject, ISpatialObject<TBounds>
        where TBounds : struct
    {
        public abstract TBounds RawBounds { get; }
        public abstract TBounds Bounds { get; }

        [SerializeField] private Color boundsColor = Color.green;

        protected abstract Vector3 GlobalBoundsCenter { get; }

        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(GlobalBoundsCenter, BoundsSize);
        }
    }

    public abstract class SpatialGameObject : MonoBehaviour
    {
        public abstract Vector3 BoundsCenter { get; }

        public abstract Vector3 BoundsSize { get; }

        public abstract Vector3 BoundsMin { get; }

        public abstract Vector3 BoundsMax { get; }
    }
}
