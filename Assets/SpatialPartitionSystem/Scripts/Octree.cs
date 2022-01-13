using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem
{
    public sealed class Octree<TObject> : SpatialTree<TObject>
        where TObject : class, ISpatialObject
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
        
        public Octree(Bounds bounds, int maxObjects, int maxDepth) : base(bounds, maxObjects, maxDepth) { }
    }
}