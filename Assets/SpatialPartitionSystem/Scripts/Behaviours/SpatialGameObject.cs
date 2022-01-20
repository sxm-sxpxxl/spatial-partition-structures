using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public abstract class SpatialGameObject<TBounds> : SpatialGameObject, ISpatialObject<TBounds>
        where TBounds : struct
    {
        [SerializeField] private Color boundsColor = Color.green;
        
        public abstract TBounds Bounds { get; }
        
        public abstract TBounds LocalBounds { get; }

        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(WorldBoundsCenter, WorldBoundsSize);
        }
    }

    public abstract class SpatialGameObject : MonoBehaviour
    {
        public abstract Vector3 WorldBoundsCenter { get; }
        
        public abstract Vector3 WorldBoundsSize { get; }

        public abstract Vector3 LocalBoundsCenter { get; }

        public abstract Vector3 BoundsSize { get; }

        public abstract Vector3 BoundsMin { get; }

        public abstract Vector3 BoundsMax { get; }
    }
}
