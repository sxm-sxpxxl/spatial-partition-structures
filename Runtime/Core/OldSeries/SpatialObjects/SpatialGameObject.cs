using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.OldSeries
{
    [DisallowMultipleComponent]
    public abstract class SpatialGameObject<TBounds> : SpatialGameObject, ISpatialObject<TBounds>
        where TBounds : struct
    {
        [Tooltip("The bounds color for debug visualization.")]
        [SerializeField] private Color boundsColor = Color.green;
        
        public abstract TBounds Bounds { get; set; }
        
        public abstract TBounds LocalBounds { get; }

        private void OnDrawGizmos()
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(WorldBoundsCenter, BoundsSize);
        }
    }

    public abstract class SpatialGameObject : MonoBehaviour
    {
        public abstract Vector3 WorldBoundsCenter { get; }
        
        public abstract Vector3 WorldBoundsSize { get; }

        public abstract Vector3 LocalBoundsCenter { get; }

        public abstract Vector3 BoundsCenter { get; set; }
        
        public abstract Vector3 BoundsSize { get; set; }

        public abstract Vector3 BoundsMin { get; }

        public abstract Vector3 BoundsMax { get; }
    }
}
