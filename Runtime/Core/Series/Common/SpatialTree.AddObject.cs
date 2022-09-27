using System;
using UnityEngine.Assertions;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        public bool TryAdd(TObject obj, TBounds objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);

            int targetNodeIndex = FindTargetNodeIndex(objBounds);
            if (targetNodeIndex == Null)
            {
                objectIndex = Null;
                return false;
            }

            objectIndex = _cachedAddObjectAction.Invoke(targetNodeIndex, obj, objBounds);
            return true;
        }
        
        internal int AddObjectToLeaf(int leafIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);
            
            if (_nodes[leafIndex].depth >= _maxDepth || _nodes[leafIndex].objectsCount < _maxLeafObjects)
            {
                return CreateObjectFor(leafIndex, obj, objBounds);
            }
            else
            {
                return Split(leafIndex, obj, objBounds);
            }
        }
        
        internal void LinkChildrenNodesTo(int nodeIndex, int firstChildNodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsTrue(firstChildNodeIndex >= 0 && firstChildNodeIndex < _nodes.Capacity);

            var node = _nodes[nodeIndex];

            node.isLeaf = false;
            node.firstChildIndex = firstChildNodeIndex;

            _nodes[nodeIndex] = node;
            
            for (int i = 0; i < _maxChildrenCount; i++)
            {
                node = _nodes[firstChildNodeIndex + i];
                node.parentIndex = nodeIndex;
                _nodes[firstChildNodeIndex + i] = node;
            }
        }
        
        internal int[] Split(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            
            int[] childrenIndexes = new int[_maxChildrenCount];
            TBounds parentBounds = _nodes[nodeIndex].bounds;
            
            for (int i = 0; i < _maxChildrenCount; i++)
            {
                _nodes.Add(new Node<TBounds, TVector>(
                    depth: (byte) (_nodes[nodeIndex].depth + 1),
                    bounds: (TBounds) parentBounds.GetChildBoundsBy((SplitSection) i)
                ), out childrenIndexes[i]);
            }

            return childrenIndexes;
        }

        private int CreateObjectFor(int leafIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);
            
            _objects.Add(new NodeObject<TObject, TBounds, TVector>(
                leafIndex: leafIndex,
                target: obj,
                bounds: objBounds
            ), out int newObjectIndex);

            LinkObjectTo(leafIndex, newObjectIndex);
            return newObjectIndex;
        }
        
        private int Split(int leafIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);

            int[] childrenIndexes = Split(leafIndex);

            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);
            int targetChildIndex;

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                targetChildIndex = FindTargetNodeIndex(childrenIndexes, _objects[unlinkedPointersIndexes[i]].bounds);
                
                Assert.IsTrue(targetChildIndex != Null);
                LinkObjectTo(targetChildIndex, unlinkedPointersIndexes[i]);
            }

            targetChildIndex = FindTargetNodeIndex(childrenIndexes, objBounds);
            Assert.IsTrue(targetChildIndex != Null);
            
            int objectIndex = AddObjectToLeaf(targetChildIndex, obj, objBounds);
            LinkChildrenNodesTo(leafIndex, childrenIndexes[0]);

            return objectIndex;
        }

        private void LinkObjectTo(int leafIndex, int objectIndex)
        {
            Assert.IsTrue(_nodes.Contains(leafIndex));
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            var obj = _objects[objectIndex];
            obj.leafIndex = leafIndex;
            _objects[objectIndex] = obj;
            
            var node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                node.firstChildIndex = objectIndex;
                node.objectsCount++;
                
                _nodes[leafIndex] = node;
                return;
            }

            int currentObjectIndex = node.firstChildIndex;
            var currentNodeObject = _objects[currentObjectIndex];

            while (currentNodeObject.nextObjectIndex != Null)
            {
                currentObjectIndex = currentNodeObject.nextObjectIndex;
                currentNodeObject = _objects[currentObjectIndex];
            }

            currentNodeObject.nextObjectIndex = objectIndex;
            _objects[currentObjectIndex] = currentNodeObject;

            node.objectsCount++;
            _nodes[leafIndex] = node;
        }
        
        private int[] UnlinkObjectPointersFrom(int leafIndex)
        {
            Assert.IsTrue(_nodes.Contains(leafIndex));
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

            var node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                return null;
            }

            int[] unlinkedObjects = new int[node.objectsCount];
            int unlinkedObjectsIndex = 0;

            int currentObjectIndex = node.firstChildIndex;
            var currentNodeObject = _objects[currentObjectIndex];

            unlinkedObjects[unlinkedObjectsIndex++] = currentObjectIndex;

            while (currentNodeObject.nextObjectIndex != Null)
            {
                int nextObjectPointerIndex = currentNodeObject.nextObjectIndex;

                currentNodeObject.nextObjectIndex = Null;
                _objects[currentObjectIndex] = currentNodeObject;

                currentObjectIndex = nextObjectPointerIndex;
                currentNodeObject = _objects[currentObjectIndex];
                
                unlinkedObjects[unlinkedObjectsIndex++] = currentObjectIndex;
            }

            node.firstChildIndex = Null;
            node.objectsCount = 0;
            _nodes[leafIndex] = node;

            return unlinkedObjects;
        }
        
        private int FindTargetNodeIndex(TBounds objBounds)
        {
            int targetNodeIndex = Null;

            TraverseFromRoot(data =>
            {
                if (data.node.bounds.Intersects(objBounds))
                {
                    targetNodeIndex = data.node.isLeaf ? data.nodeIndex : targetNodeIndex;
                    return ExecutionSignal.ContinueInDepth;
                }

                return ExecutionSignal.Continue;
            });
            
            return targetNodeIndex;
        }
        
        private int FindTargetNodeIndex(int[] nodeIndexes, TBounds objBounds)
        {
            Assert.IsNotNull(nodeIndexes);
            
            for (int i = 0; i < nodeIndexes.Length; i++)
            {
                if (_nodes[nodeIndexes[i]].bounds.Intersects(objBounds))
                {
                    return nodeIndexes[i];
                }
            }

            return Null;
        }
    }
}
