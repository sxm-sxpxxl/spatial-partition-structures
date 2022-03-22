using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
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
            
            objectIndex = AddObjectToLeaf(targetNodeIndex, obj, objBounds);
            return true;
        }
        
        public int AddObjectToLeaf(int leafIndex, TObject obj, TBounds objBounds)
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
        
        internal void LinkChildrenNodesTo(int nodeIndex, int firstChildIndex)
        {
            Assert.IsTrue(firstChildIndex >= 0 && firstChildIndex < _nodes.Capacity);
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            var node = _nodes[nodeIndex];

            node.isLeaf = false;
            node.firstChildIndex = firstChildIndex;

            _nodes[nodeIndex] = node;
            
            for (int i = 0; i < _maxChildrenCount; i++)
            {
                node = _nodes[firstChildIndex + i];
                node.parentIndex = nodeIndex;
                _nodes[firstChildIndex + i] = node;
            }
        }
        
        internal int[] Split(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            
            int[] childrenIndexes = new int[_maxChildrenCount];
            TBounds parentBounds = _nodes[nodeIndex].bounds;
            
            for (int i = 0; i < _maxChildrenCount; i++)
            {
                _nodes.Add(new Node<TBounds, TVector>
                {
                    parentIndex = Null,
                    firstChildIndex = Null,
                    objectsCount = 0,
                    isLeaf = true,
                    depth = (byte) (_nodes[nodeIndex].depth + 1),
                    bounds = (TBounds) parentBounds.GetChildBoundsBy((SplitSection) i)
                }, out childrenIndexes[i]);
            }

            return childrenIndexes;
        }

        internal int FindTargetNodeIndex(TBounds objBounds)
        {
            int targetNodeIndex = Null;

            TraverseFromRoot(data =>
            {
                if (_nodes[data.nodeIndex].bounds.Intersects(objBounds))
                {
                    targetNodeIndex = data.nodeIndex;
                    return ExecutionSignal.ContinueInDepth;
                }

                return ExecutionSignal.Continue;
            });
            
            return targetNodeIndex;
        }
        
        private int CreateObjectFor(int leafIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);
            
            _objects.Add(new NodeObject<TObject, TBounds, TVector>
            {
                leafIndex = leafIndex,
                target = obj,
                bounds = objBounds
            }, out int newObjectIndex);
            
            _objectPointers.Add(new ObjectPointer
            {
                objectIndex = newObjectIndex,
                nextObjectPointerIndex = Null
            }, out int newObjectPointerIndex);

            LinkObjectPointerTo(leafIndex, newObjectPointerIndex);
            
            return newObjectIndex;
        }
        
        private int Split(int leafIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);

            int[] childrenIndexes = Split(leafIndex);

            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);
            int unlinkedObjectIndex, targetChildIndex;

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                unlinkedObjectIndex = _objectPointers[unlinkedPointersIndexes[i]].objectIndex;
                targetChildIndex = FindTargetNodeIndex(childrenIndexes, _objects[unlinkedObjectIndex].bounds);

                if (targetChildIndex == Null)
                {
                    int a = FindTargetNodeIndex(childrenIndexes, _objects[unlinkedObjectIndex].bounds);
                }
                
                LinkObjectPointerTo(targetChildIndex, unlinkedPointersIndexes[i]);
            }

            targetChildIndex = FindTargetNodeIndex(childrenIndexes, objBounds);
            
            if (targetChildIndex == Null)
            {
                return Null;
            }
            
            int objectIndex = AddObjectToLeaf(targetChildIndex, obj, objBounds);
            LinkChildrenNodesTo(leafIndex, childrenIndexes[0]);

            return objectIndex;
        }

        private void LinkObjectPointerTo(int leafIndex, int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

            int objectIndex = _objectPointers[objectPointerIndex].objectIndex;
            var obj = _objects[objectIndex];
            
            obj.leafIndex = leafIndex;
            _objects[objectIndex] = obj;
            
            var node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                node.firstChildIndex = objectPointerIndex;
                node.objectsCount++;
                
                _nodes[leafIndex] = node;
                return;
            }

            int currentPointerIndex = node.firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            while (currentPointer.nextObjectPointerIndex != Null)
            {
                currentPointerIndex = currentPointer.nextObjectPointerIndex;
                currentPointer = _objectPointers[currentPointerIndex];
            }

            currentPointer.nextObjectPointerIndex = objectPointerIndex;
            _objectPointers[currentPointerIndex] = currentPointer;

            node.objectsCount++;
            _nodes[leafIndex] = node;
        }
        
        private int[] UnlinkObjectPointersFrom(int leafIndex)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

            var node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                return null;
            }

            int[] unlinkedObjects = new int[node.objectsCount];
            int unlinkedObjectsIndex = 0;

            int currentPointerIndex = node.firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            unlinkedObjects[unlinkedObjectsIndex++] = currentPointerIndex;

            while (currentPointer.nextObjectPointerIndex != Null)
            {
                int nextObjectPointerIndex = currentPointer.nextObjectPointerIndex;

                currentPointer.nextObjectPointerIndex = Null;
                _objectPointers[currentPointerIndex] = currentPointer;

                currentPointerIndex = nextObjectPointerIndex;
                currentPointer = _objectPointers[currentPointerIndex];
                
                unlinkedObjects[unlinkedObjectsIndex++] = currentPointerIndex;
            }

            node.firstChildIndex = Null;
            node.objectsCount = 0;
            _nodes[leafIndex] = node;

            return unlinkedObjects;
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
