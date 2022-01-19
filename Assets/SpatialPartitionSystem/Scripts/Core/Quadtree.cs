using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public sealed class Quadtree<TObject> : SpatialTree<TObject, Rect>
        where TObject : class, ISpatialObject<Rect>
    {
        protected override Dictionary<int, Func<Vector3, Vector3, Vector3>> QuadrantOrientationMap { get; }
            = new Dictionary<int, Func<Vector3, Vector3, Vector3>>
            {
                { 0, (min, max) => new Vector3(max.x, max.y) },
                { 1, (min, max) => new Vector3(min.x, max.y) },
                { 2, (min, max) => new Vector3(min.x, min.y) },
                { 3, (min, max) => new Vector3(max.x, min.y) }
            };

        private readonly Dictionary<PlaneOrientation, Tuple<int, int>> planeOrientationMap = new Dictionary<PlaneOrientation, Tuple<int, int>>
        {
            { PlaneOrientation.XY, new Tuple<int, int>(0, 1) },
            { PlaneOrientation.YZ, new Tuple<int, int>(1, 2) },
            { PlaneOrientation.XZ, new Tuple<int, int>(0, 2) }
        };

        private PlaneOrientation _currentPlaneOrientation;
        
        /// <summary>
        /// Constructs a new quadtree with given settings.
        /// </summary>
        /// <param name="bounds">The rect bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        /// <param name="planeOrientation">The initial plane orientation of the tree rect bounds.</param>
        public Quadtree(Rect bounds, int maxObjects, int maxDepth,
            PlaneOrientation planeOrientation = PlaneOrientation.XY) : base(bounds, maxObjects, maxDepth)
        {
            _currentPlaneOrientation = planeOrientation;
        }

        /// <summary>
        /// Sets the plane orientation of the tree bounds.
        /// </summary>
        /// <param name="value">XY, YZ or XZ</param>
        public void SetOrientation(PlaneOrientation value)
        {
            _currentPlaneOrientation = value;
        }
        
        public override bool Contains(Rect bounds) => _root.Bounds.Contains(bounds.min) && _root.Bounds.Contains(bounds.max);
        
        protected override bool Intersects(Rect a, Rect b) => a.Overlaps(b);

        protected override Vector3 GetBoundsCenter(Rect bounds) => ReorientVector(bounds.center);

        protected override Vector3 GetBoundsSize(Rect bounds) => ReorientVector(bounds.size);

        protected override Rect CreateBounds(Vector3 center, Vector3 size) =>
            new Rect(Vector2.zero, ReorientVector(size)) { center = ReorientVector(center) };

        private Vector3 ReorientVector(Vector2 initial)
        {
            Vector3 result = Vector3.zero;
            
            var (index1, index2) = planeOrientationMap[_currentPlaneOrientation];

            result[index1] = initial.x;
            result[index2] = initial.y;

            return result;
        }
    }
}
