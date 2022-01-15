using NUnit.Framework;
using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Tests {
    public class SpatialTreeTests
    {
        private class TestSpatialObject : ISpatialObject
        {
            public Bounds Bounds { get; }

            public TestSpatialObject(Bounds bounds)
            {
                Bounds = bounds;
            }
        }
    
        [Test]
        public void ShallowAddition()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector3.one);
            SpatialTree<TestSpatialObject> tree = new Octree<TestSpatialObject>(treeBounds, 1, 8);
            var testObj = new TestSpatialObject(new Bounds(treeBounds.center + 0.5f * treeBounds.extents, 0.1f * treeBounds.size));

            bool isAdded = tree.TryAdd(testObj);
            Assert.IsTrue(isAdded);
            Assert.IsTrue(tree.Contains(testObj));
        }
    }
}
