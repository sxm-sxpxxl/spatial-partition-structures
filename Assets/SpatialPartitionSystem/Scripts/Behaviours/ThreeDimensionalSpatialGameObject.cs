using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public sealed class ThreeDimensionalSpatialGameObject : SpatialGameObject<Bounds>
    {
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        
        public override Bounds RawBounds => new Bounds(bounds.center, BoundsSize);

        public override Bounds Bounds => new Bounds(BoundsCenter, BoundsSize);

        public override Vector3 BoundsCenter => transform.localPosition + bounds.center;

        public override Vector3 BoundsSize => bounds.size;

        public override Vector3 BoundsMin => transform.localPosition + bounds.min;

        public override Vector3 BoundsMax => transform.localPosition + bounds.max;

        protected override Vector3 GlobalBoundsCenter => transform.position + bounds.center;
    }
}
