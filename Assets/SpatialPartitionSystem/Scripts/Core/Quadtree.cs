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

        /// <summary>
        /// Constructs a new quadtree with given settings.
        /// </summary>
        /// <param name="bounds">The rect bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        public Quadtree(Rect bounds, int maxObjects, int maxDepth) : base(bounds, maxObjects, maxDepth) { }

        public override bool Contains(Rect bounds) => _root.Bounds.Contains(bounds.min) && _root.Bounds.Contains(bounds.max);
        
        protected override bool Intersects(Rect a, Rect b) => a.Overlaps(b);

        protected override Vector3 GetBoundsCenter(Rect bounds) => bounds.center;

        protected override Vector3 GetBoundsSize(Rect bounds) => bounds.size;

        protected override Rect CreateBounds(Vector3 center, Vector3 size) => new Rect(Vector2.zero, size) { center = center };
    }
}
