using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.OldSeries
{
    public sealed class Octree<TObject> : SpatialTree<TObject, Bounds>
        where TObject : class, ISpatialObject<Bounds>
    {
        protected override Dictionary<int, Func<Vector3, Vector3, Vector3>> QuadrantOrientationMap { get; }
            = new Dictionary<int, Func<Vector3, Vector3, Vector3>>
            {
                { 0, (min, max) => new Vector3(max.x, max.y, max.z) },
                { 1, (min, max) => new Vector3(min.x, max.y, max.z) },
                { 2, (min, max) => new Vector3(min.x, min.y, max.z) },
                { 3, (min, max) => new Vector3(max.x, min.y, max.z) },
                { 4, (min, max) => new Vector3(max.x, max.y, min.z) },
                { 5, (min, max) => new Vector3(min.x, max.y, min.z) },
                { 6, (min, max) => new Vector3(min.x, min.y, min.z) },
                { 7, (min, max) => new Vector3(max.x, min.y, min.z) }
            };

        /// <summary>
        /// Constructs a new octree with given settings.
        /// </summary>
        /// <param name="bounds">The bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        public Octree(Bounds bounds, int maxObjects, int maxDepth) : base(bounds, maxObjects, maxDepth) { }

        public override bool Contains(Bounds bounds) => _root.Bounds.Contains(bounds.min) && _root.Bounds.Contains(bounds.max);

        protected override bool Intersects(Bounds a, Bounds b)
        {
            Vector3 aMin = a.min;
            Vector3 aMax = a.max;
            Vector3 bMin = b.min;
            Vector3 bMax = b.max;
            
            return aMin.x < bMax.x && aMax.x > bMin.x && aMin.y < bMax.y && aMax.y > bMin.y && aMin.z < bMax.z && aMax.z > bMin.z;
        }

        protected override Vector3 GetBoundsCenter(Bounds bounds) => bounds.center;

        protected override Vector3 GetBoundsSize(Bounds bounds) => bounds.size;

        protected override Bounds CreateBounds(Vector3 center, Vector3 size) => new Bounds(center, size);
    }
}
