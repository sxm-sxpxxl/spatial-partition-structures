using System;
using System.Collections.Generic;
using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Behaviours
{
    public sealed class TwoDimensionalSpatialGameObject : SpatialGameObject<Rect>
    {
        [SerializeField] private Rect bounds = new Rect(Vector2.zero, Vector2.one) { center = Vector2.zero };
        [SerializeField] private PlaneOrientation planeOrientation = PlaneOrientation.XY;

        private readonly Dictionary<PlaneOrientation, Quaternion> planeOrientationToRotationMap =
            new Dictionary<PlaneOrientation, Quaternion>
            {
                {PlaneOrientation.XY, Quaternion.Euler(0f, 0f, 0f)},
                {PlaneOrientation.YZ, Quaternion.Euler(0f, 90f, 0f)},
                {PlaneOrientation.XZ, Quaternion.Euler(90f, 0f, 0f)}
            };
        
        private enum PlaneOrientation
        {
            XY,
            YZ,
            XZ
        }

        private void OnValidate()
        {
            transform.rotation = planeOrientationToRotationMap[planeOrientation];
        }

        public override Rect Bounds => new Rect(Vector3.zero, BoundsSize) { center = (Vector3) bounds.center };
        
        public override Rect LocalBounds => new Rect(Vector2.zero, BoundsSize) { center = LocalBoundsCenter };

        public override Vector3 LocalBoundsCenter => transform.localPosition + (Vector3) bounds.center;

        public override Vector3 BoundsSize => bounds.size;

        public override Vector3 BoundsMin =>  bounds.min;

        public override Vector3 BoundsMax => bounds.max;
        
        protected override Vector3 WorldBoundsCenter => transform.TransformPoint(bounds.center);

        protected override Vector3 WorldBoundsSize => transform.TransformDirection(bounds.size);
    }
}
