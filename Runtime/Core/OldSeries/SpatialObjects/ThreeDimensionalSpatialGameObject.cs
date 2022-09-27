using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.OldSeries
{
    public class ThreeDimensionalSpatialGameObject : SpatialGameObject<Bounds>
    {
        [Tooltip("The rect bounds of that spatial object.")]
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        public override Bounds Bounds
        {
            get => new Bounds(bounds.center, BoundsSize);
            set => bounds = value;
        }

        public override Bounds LocalBounds => new Bounds(LocalBoundsCenter, BoundsSize);

        public override Vector3 WorldBoundsCenter => transform.TransformPoint(bounds.center);

        public override Vector3 WorldBoundsSize => transform.TransformDirection(bounds.size);
        
        public override Vector3 LocalBoundsCenter => transform.localPosition + bounds.center;

        public override Vector3 BoundsCenter
        {
            get => bounds.center;
            set => bounds.center = value;
        }
        
        public override Vector3 BoundsSize
        {
            get => bounds.size;
            set => bounds.size = value;
        }

        public override Vector3 BoundsMin => bounds.min;

        public override Vector3 BoundsMax => bounds.max;
    }
}
