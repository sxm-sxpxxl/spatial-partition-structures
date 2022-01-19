using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public sealed class TwoDimensionalSpatialGameObject : SpatialGameObject<Rect>
    {
        [SerializeField] private Rect bounds = new Rect(Vector2.zero, Vector2.one) { center = Vector2.zero };
        
        public override Rect RawBounds => new Rect(Vector3.zero, BoundsSize) { center = (Vector3) bounds.center };
        
        public override Rect Bounds => new Rect(Vector2.zero, BoundsSize) { center = BoundsCenter };

        public override Vector3 BoundsCenter => transform.localPosition + (Vector3) bounds.center;

        public override Vector3 BoundsSize => bounds.size;

        public override Vector3 BoundsMin =>  transform.localPosition + (Vector3) bounds.min;

        public override Vector3 BoundsMax => transform.localPosition + (Vector3) bounds.max;

        protected override Vector3 GlobalBoundsCenter => transform.position + (Vector3) bounds.center;
    }
}
