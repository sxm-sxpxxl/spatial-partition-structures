using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public sealed class Quadtree<TObject> : SpatialTree<TObject>
        where TObject : class, ISpatialObject
    {
        protected override Dictionary<int, Func<Vector3, Vector3, Vector3>> QuadrantOrientationMap { get; }
            = new Dictionary<int, Func<Vector3, Vector3, Vector3>>
            {
                { 0, (min, max) => new Vector3(max.x, max.y) },
                { 1, (min, max) => new Vector3(min.x, max.y) },
                { 2, (min, max) => new Vector3(min.x, min.y) },
                { 3, (min, max) => new Vector3(max.x, min.y) }
            };
        
        public Quadtree(Bounds bounds, int maxObjects, int maxDepth) : base(bounds, maxObjects, maxDepth) { }
    }
}
