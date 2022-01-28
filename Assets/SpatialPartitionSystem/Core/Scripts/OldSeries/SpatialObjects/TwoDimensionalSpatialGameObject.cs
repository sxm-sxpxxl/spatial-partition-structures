using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.OldSeries
{
    public class TwoDimensionalSpatialGameObject : SpatialGameObject<Rect>
    {
        [Tooltip("The rect bounds of that spatial object.")]
        [SerializeField] private Rect bounds = new Rect(Vector2.zero, Vector2.one) { center = Vector2.zero };
        [Tooltip("The plane orientation for debug visualization.")]
        [SerializeField] private PlaneOrientation planeOrientation = PlaneOrientation.XY;

        private readonly Dictionary<PlaneOrientation, Quaternion> _planeOrientationToRotationMap =
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
            transform.rotation = _planeOrientationToRotationMap[planeOrientation];
        }

        public override Rect Bounds
        {
            get => new Rect(Vector3.zero, BoundsSize) { center = (Vector3) bounds.center };
            set => bounds = value;
        }

        public override Rect LocalBounds => new Rect(Vector2.zero, BoundsSize) { center = LocalBoundsCenter };

        public override Vector3 WorldBoundsCenter => transform.TransformPoint(bounds.center);

        public override Vector3 WorldBoundsSize => transform.TransformDirection(bounds.size);
        
        public override Vector3 LocalBoundsCenter => transform.localPosition + (Vector3) bounds.center;

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

        public override Vector3 BoundsMin =>  bounds.min;

        public override Vector3 BoundsMax => bounds.max;
    }
}
