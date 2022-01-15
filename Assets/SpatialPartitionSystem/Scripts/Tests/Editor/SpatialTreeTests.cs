using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SpatialPartitionSystem.Core;

namespace SpatialPartitionSystem.Tests {
    public class SpatialTreeTests
    {
        private class TestSpatialObject : ISpatialObject
        {
            public Bounds Bounds { get; set; }

            public TestSpatialObject(Bounds bounds)
            {
                Bounds = bounds;
            }
        }
    
        [Test]
        public void AddObjects()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 3);

            var topRightOffset = 0.75f * treeBounds.extents;
            var bottomLeftOffset = 0.25f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));
            var testObj3 = new TestSpatialObject(new Bounds(treeBounds.center + bottomLeftOffset, objBoundsSize));
            var testObj4 = new TestSpatialObject(new Bounds(treeBounds.center + new Vector3(topRightOffset.x, -topRightOffset.y), objBoundsSize));

            var testObjects = new [] { testObj1, testObj2 };

            foreach (var obj in testObjects)
            {
                bool isAdded = tree.TryAdd(obj);
                
                Assert.IsTrue(isAdded);
                Assert.IsTrue(tree.Contains(obj));
            }
            
            // Check that after adding the objects the tree is correctly splitted
            var childNodes = tree.GetNodesFor(depth: 1);
            Assert.AreEqual(4, childNodes.Count);

            // Check the location and size of the bounds of new nodes after splitting
            {
                var expectedBoundsSize = 0.5f * treeBounds.size;
                var boundsOffset = 0.5f * expectedBoundsSize;

                Assert.AreEqual(childNodes[0].Bounds.center, treeBounds.center + boundsOffset);
                Assert.AreEqual(childNodes[0].Bounds.size, expectedBoundsSize);
            
                Assert.AreEqual(childNodes[1].Bounds.center, treeBounds.center + new Vector3(-boundsOffset.x, boundsOffset.y));
                Assert.AreEqual(childNodes[1].Bounds.size, expectedBoundsSize);
            
                Assert.AreEqual(childNodes[2].Bounds.center, treeBounds.center - boundsOffset);
                Assert.AreEqual(childNodes[2].Bounds.size, expectedBoundsSize);
            
                Assert.AreEqual(childNodes[3].Bounds.center, treeBounds.center + new Vector3(boundsOffset.x, -boundsOffset.y));
                Assert.AreEqual(childNodes[3].Bounds.size, expectedBoundsSize);   
            }

            // Check the location of objects at the initial splitting depth
            Assert.IsTrue(childNodes[0].Objects.Contains(testObj1));
            Assert.IsTrue(childNodes[2].Objects.Contains(testObj2));

            // Add the third object next to the first
            tree.TryAdd(testObj3);
            
            // Check the migration of data from the parent node to the child
            Assert.IsFalse(childNodes[0].Objects.Contains(testObj1));
            Assert.IsNotNull(childNodes[0].Childrens);

            // Check the location of objects at the new splitting depth
            Assert.IsTrue(childNodes[0].Childrens[0].Objects.Contains(testObj1));
            Assert.IsTrue(childNodes[0].Childrens[2].Objects.Contains(testObj3));

            // Add the fourth object to the smallest depth free node
            tree.TryAdd(testObj4);
            
            // Check that no new nodes are created under the object, since it completely fits into the parent
            Assert.IsNull(childNodes[3].Childrens);
            Assert.IsTrue(childNodes[3].Objects.Contains(testObj4));
        }
        
        [Test]
        public void ClearObjects()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 2);

            var topRightOffset = 0.75f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));
            
            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);

            var allNodes = tree.GetAllNodes();
            Assert.AreEqual(5, allNodes.Count);

            tree.Clear();

            allNodes = tree.GetAllNodes();
            Assert.AreEqual(1, allNodes.Count);
            Assert.IsFalse(allNodes[0].Objects.Contains(testObj1));
            Assert.IsFalse(allNodes[0].Objects.Contains(testObj2));
        }

        [Test]
        public void MaxDepthOfTree()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 0);

            var topRightOffset = 0.75f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));
            
            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);
            
            var allNodes = tree.GetAllNodes();
            Assert.AreEqual(1, allNodes.Count);
            
            Assert.IsTrue(allNodes[0].Objects.Contains(testObj1));
            Assert.IsTrue(allNodes[0].Objects.Contains(testObj2));
        }
        
        [Test]
        public void MaxObjectsPerNode()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 2, 2);

            var topRightOffset = 0.75f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));

            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);

            var allNodes = tree.GetAllNodes();
            Assert.AreEqual(1, allNodes.Count);
            
            Assert.IsTrue(allNodes[0].Objects.Contains(testObj1));
            Assert.IsTrue(allNodes[0].Objects.Contains(testObj2));
        }

        [Test]
        public void RemoveObject()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 2);

            var topRightOffset = 0.75f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));

            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);

            // Check that adding objects leads to splitting
            Assert.AreEqual(4, tree.GetNodesFor(1).Count);
            
            bool isRemoved = tree.TryRemove(testObj2);
            Assert.IsTrue(isRemoved);

            // Check that the object is really removed and the released space is merged
            var allNodes = tree.GetAllNodes();
            Assert.AreEqual(1, allNodes.Count);
            Assert.IsTrue(allNodes[0].Objects.Contains(testObj1));
            Assert.IsFalse(allNodes[0].Objects.Contains(testObj2));
        }

        [Test]
        public void UpdateObject()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 2);

            var topRightOffset = 0.75f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + topRightOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - topRightOffset, objBoundsSize));
            
            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);

            var childNodes = tree.GetNodesFor(1);
            Assert.IsTrue(childNodes[0].Objects.Contains(testObj1));
            
            testObj1.Bounds = new Bounds(treeBounds.center + new Vector3(-topRightOffset.x, topRightOffset.y), objBoundsSize);
            tree.Update(testObj1);

            // Check that the object is located in the tree in accordance with new position of its bounds
            Assert.IsFalse(childNodes[0].Objects.Contains(testObj1));
            Assert.IsTrue(childNodes[1].Objects.Contains(testObj1));
        }

        [Test]
        public void QueryObjects()
        {
            var treeBounds = new Bounds(Vector3.zero, Vector2.one);
            SpatialTree<TestSpatialObject> tree = new Quadtree<TestSpatialObject>(treeBounds, 1, 2);

            var queryBounds = new Bounds(Vector3.zero, 0.5f * Vector2.one);
            
            var bottomLeftOffset = 0.25f * treeBounds.extents;
            var objBoundsSize = 0.1f * treeBounds.size;
            
            var testObj1 = new TestSpatialObject(new Bounds(treeBounds.center + bottomLeftOffset, objBoundsSize));
            var testObj2 = new TestSpatialObject(new Bounds(treeBounds.center - bottomLeftOffset, objBoundsSize));

            tree.TryAdd(testObj1);
            tree.TryAdd(testObj2);
            
            Assert.AreEqual(2, tree.Query(queryBounds, 2).ToArray().Length);

            queryBounds.center += (Vector3) (0.3f * Vector2.one);
            var queryObjects = tree.Query(queryBounds, 2).ToArray();
            
            Assert.AreEqual(1, queryObjects.Length);
            Assert.AreEqual(testObj1, queryObjects[0]);
        }
    }
}
