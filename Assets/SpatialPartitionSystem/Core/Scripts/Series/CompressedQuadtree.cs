using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class CompressedQuadtree<TObject> where TObject : class
    {
        private const sbyte NULL = -1;
        private const sbyte MAX_POSSIBLE_DEPTH = 8;

        private readonly FreeList<Node> _nodes;
        private readonly FreeList<ObjectPointer> _objectPointers;
        private readonly FreeList<NodeObject<TObject>> _objects;

        private readonly sbyte _maxLeafObjects, _maxDepth;
        private readonly int _rootIndex;
        
        private bool[] _missingObjects;
        private readonly List<TObject> _queryObjects;

        internal int RootIndex => _rootIndex;
        
        private int CurrentBranchCount => _nodes.Count / 4;

        private struct CompressData
        {
            public int nodeIndex;
            public int firstInterestingNodeIndex;
            public int[] notInterestingNodeIndexes;

            public bool HasNotInterestingNodeWith(int targetNodeIndex)
            {
                if (notInterestingNodeIndexes == null)
                {
                    return false;
                }
                
                for (int i = 0; i < notInterestingNodeIndexes.Length; i++)
                {
                    if (notInterestingNodeIndexes[i] == targetNodeIndex)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal struct TraverseData
        {
            public int nodeIndex;
            public int parentIndex;
            public int childOffset;

            public TraverseData(int nodeIndex, int parentIndex, int childOffset)
            {
                this.nodeIndex = nodeIndex;
                this.parentIndex = parentIndex;
                this.childOffset = childOffset;
            }
        }
        
        internal enum ExecutionSignal
        {
            Continue,
            ContinueInDepth,
            Stop
        }
        
        private enum QuadrantNumber
        {
            One = 0,
            Two = 1,
            Three = 2,
            Four = 3
        }

        public CompressedQuadtree(AABB2D bounds, sbyte maxLeafObjects, sbyte maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = (sbyte) Mathf.Min(maxDepth, MAX_POSSIBLE_DEPTH);

            int maxNodesCount = (int) Mathf.Pow(4, _maxDepth);
        
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);
            _missingObjects = new bool[initialObjectsCapacity];
            _queryObjects = new List<TObject>(capacity: initialObjectsCapacity);
            
            _nodes.Add(new Node
            {
                firstChildIndex = NULL,
                objectsCount = 0,
                isLeaf = true,
                depth = 0,
                bounds = bounds
            }, out _rootIndex);
        }

        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();
            TraverseFromRoot(data =>
            {
                var busyColor = _nodes[data.nodeIndex].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                var worldCenter = relativeTransform.TransformPoint(_nodes[data.nodeIndex].bounds.Center);
                var worldSize = relativeTransform.TransformPoint(_nodes[data.nodeIndex].bounds.Size);

                drawer.DrawWireCube(worldCenter, worldSize);
                
                return ExecutionSignal.Continue;
            });
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int targetNodeIndex = FindTargetNodeIndex(objBounds);
            return TryAdd(targetNodeIndex, obj, objBounds, out objectIndex);
        }

        internal bool TryAdd(int nodeIndex, TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            Assert.IsTrue((nodeIndex >= 0 && nodeIndex < _nodes.Capacity) || nodeIndex == NULL);

            if (nodeIndex == NULL)
            {
                objectIndex = NULL;
                return false;
            }
            
            if (_nodes[nodeIndex].isLeaf)
            {
                objectIndex = Add(nodeIndex, obj, objBounds);
                Compress(nodeIndex);
            }
            else
            {
                int decompressedTargetLeafIndex = Decompress(nodeIndex, objBounds);
                objectIndex = Add(decompressedTargetLeafIndex, obj, objBounds);
                Compress(nodeIndex);
            }
            
            return true;
        }

        public bool TryRemove(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            if (linkedLeafIndex == NULL)
            {
                return false;
            }
            
            Remove(objectIndex);
            return true;
        }

        // todo: refactoring?
        public void CleanUp()
        {
            if (CurrentBranchCount == 0)
            {
                return;
            }

            Compress();

            bool isNeedAnotherCleanUp;
            do
            {
                isNeedAnotherCleanUp = false;
                
                int currentParentIndex = NULL, childrenObjectsCount = 0;
                bool hasBranchAmongChildren = false;
                
                TraverseFromRoot(data =>
                {
                    if (data.parentIndex == NULL)
                    {
                        return ExecutionSignal.Continue;
                    }
                    
                    if (currentParentIndex != data.parentIndex)
                    {
                        currentParentIndex = data.parentIndex;
                        childrenObjectsCount = 0;
                        hasBranchAmongChildren = false;
                    }
                    
                    if (hasBranchAmongChildren == false && _nodes[data.nodeIndex].isLeaf)
                    {
                        childrenObjectsCount += _nodes[data.nodeIndex].objectsCount;
                    }
                    else
                    {
                        hasBranchAmongChildren = true;
                    }

                    if (data.childOffset == 3 && hasBranchAmongChildren == false && childrenObjectsCount <= _maxLeafObjects)
                    {
                        CleanUp(currentParentIndex, childrenObjectsCount);
                        isNeedAnotherCleanUp = true;
                    }
                
                    return ExecutionSignal.Continue;
                });
            } while (isNeedAnotherCleanUp);
        }

        public int Update(int objectIndex, TObject updatedObj, AABB2D updatedObjBounds)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);
            Assert.IsNotNull(updatedObj);

            int newObjectIndex;
            
            if (IsObjectMissing(objectIndex))
            {
                if (TryAdd(updatedObj, updatedObjBounds, out newObjectIndex) == false)
                {
                    return objectIndex;
                }
                
                _missingObjects[objectIndex] = false;
                return newObjectIndex;
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);

            if (_nodes[linkedLeafIndex].bounds.Intersects(updatedObjBounds))
            {
                NodeObject<TObject> nodeObject = _objects[objectIndex];

                nodeObject.target = updatedObj;
                nodeObject.bounds = updatedObjBounds;
                
                _objects[objectIndex] = nodeObject;
                
                return objectIndex;
            }
                
            Remove(objectIndex);

            if (TryAdd(updatedObj, updatedObjBounds, out newObjectIndex) == false)
            {
                _missingObjects[objectIndex] = true;
                return objectIndex;
            }

            return newObjectIndex;
        }

        public IReadOnlyList<TObject> Query(AABB2D queryBounds)
        {
            _queryObjects.Clear();
            
            TraverseFromRoot(data =>
            {
                if (_nodes[data.nodeIndex].isLeaf == false || queryBounds.Intersects(_nodes[data.nodeIndex].bounds) == false)
                {
                    return ExecutionSignal.Continue;
                }
            
                TraverseObjects(data.nodeIndex, objectIndex =>
                {
                    if (queryBounds.Intersects(_objects[objectIndex].bounds))
                    {
                        _queryObjects.Add(_objects[objectIndex].target);
                    }
                });

                return ExecutionSignal.Continue;
            });

            return _queryObjects;
        }

        internal void TraverseFrom(int nodeIndex, Func<TraverseData, ExecutionSignal> eachNodeAction)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            
            if (_nodes[nodeIndex].isLeaf)
            {
                eachNodeAction.Invoke(new TraverseData(nodeIndex, NULL, 0));
                return;
            }
            
            int[] branchesIndexes = CreateEmptyIndexesArrayByLevel(CurrentBranchCount);
            branchesIndexes[0] = nodeIndex;
            
            ExecutionSignal executionSignal = eachNodeAction.Invoke(new TraverseData(nodeIndex, NULL, 0));
            if (executionSignal == ExecutionSignal.Stop)
            {
                return;
            }
            
            int parentIndex, firstChildIndex, childIndex;

            for (int traverseBranchIndex = 0, freeBranchIndex = 1;
                traverseBranchIndex < branchesIndexes.Length && branchesIndexes[traverseBranchIndex] != NULL;
                traverseBranchIndex++)
            {
                parentIndex = branchesIndexes[traverseBranchIndex];
                firstChildIndex = _nodes[parentIndex].firstChildIndex;
                
                for (int i = 0; i < 4; i++)
                {
                    childIndex = firstChildIndex + i;

                    executionSignal = eachNodeAction.Invoke(new TraverseData(childIndex, parentIndex, i));
                    if (executionSignal == ExecutionSignal.Stop)
                    {
                        return;
                    }

                    if (_nodes[childIndex].isLeaf == false)
                    {
                        branchesIndexes[freeBranchIndex++] = childIndex;
                    }
                    
                    if (executionSignal == ExecutionSignal.ContinueInDepth)
                    {
                        for (int j = traverseBranchIndex + 1; j < freeBranchIndex; j++)
                        {
                            branchesIndexes[j] = NULL;
                        }

                        freeBranchIndex = traverseBranchIndex + 1;
                        
                        if (_nodes[childIndex].isLeaf == false)
                        {
                            branchesIndexes[freeBranchIndex++] = childIndex;
                        }
                        
                        break;
                    }
                }
            }
        }

        internal Node GetNodeBy(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            return _nodes[nodeIndex];
        }

        internal TObject GetObjectBy(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);
            return _objects[objectIndex].target;
        }

        private void TraverseObjects(int nodeIndex, Action<int> objectAction = null)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[nodeIndex].isLeaf);

            if (_nodes[nodeIndex].objectsCount == 0)
            {
                return;
            }

            int currentPointerIndex = _nodes[nodeIndex].firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];
            
            objectAction?.Invoke(currentPointer.objectIndex);

            while (currentPointer.nextObjectPointerIndex != NULL)
            {
                currentPointerIndex = currentPointer.nextObjectPointerIndex;
                currentPointer = _objectPointers[currentPointerIndex];
                
                objectAction?.Invoke(currentPointer.objectIndex);
            }
        }

        private bool IsObjectMissing(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            if (objectIndex >= _missingObjects.Length)
            {
                bool[] newArray = new bool[_objects.Capacity];
                _missingObjects.CopyTo(newArray, 0);
                _missingObjects = newArray;
            }

            return _missingObjects[objectIndex];
        }

        private int Add(int leafIndex, TObject obj, AABB2D objBounds)
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
        
        private void Remove(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            int leafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            
            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                if (_objectPointers[unlinkedPointersIndexes[i]].objectIndex == objectIndex)
                {
                    DeleteObject(unlinkedPointersIndexes[i]);
                }
                else
                {
                    LinkObjectPointerTo(leafIndex, unlinkedPointersIndexes[i]);
                }
            }
        }

        private int Split(int leafIndex, TObject obj, AABB2D objBounds)
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

                LinkObjectPointerTo(targetChildIndex, unlinkedPointersIndexes[i]);
            }

            targetChildIndex = FindTargetNodeIndex(childrenIndexes, objBounds);
            
            if (targetChildIndex == NULL)
            {
                return NULL;
            }
            
            int objectIndex = Add(targetChildIndex, obj, objBounds);
            LinkChildrenNodesTo(leafIndex, childrenIndexes[0]);

            return objectIndex;
        }

        private int[] Split(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            
            int[] childrenIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                _nodes.Add(new Node
                {
                    firstChildIndex = NULL,
                    objectsCount = 0,
                    isLeaf = true,
                    depth = (sbyte) (_nodes[nodeIndex].depth + 1),
                    bounds = GetBoundsFor(_nodes[nodeIndex].bounds, (QuadrantNumber) i)
                }, out childrenIndexes[i]);
            }

            return childrenIndexes;
        }

        private void CleanUp(int parentBranchIndex, int childrenObjectsCount)
        {
            int firstChildIndex = _nodes[parentBranchIndex].firstChildIndex;
            int[] childrenUnlinkedPointerIndexes = new int[childrenObjectsCount];
            
            for (int i = 0, j = 0; i < 4; i++)
            {
                int[] unlinkedPointerIndexes = UnlinkObjectPointersFrom(firstChildIndex + i);

                if (unlinkedPointerIndexes == null)
                {
                    continue;
                }

                for (int k = 0; k < unlinkedPointerIndexes.Length; k++)
                {
                    childrenUnlinkedPointerIndexes[j++] = unlinkedPointerIndexes[k];
                }
            }

            DeleteChildrenNodesFor(parentBranchIndex);

            for (int i = 0; i < childrenUnlinkedPointerIndexes.Length; i++)
            {
                LinkObjectPointerTo(parentBranchIndex, childrenUnlinkedPointerIndexes[i]);
            }
        }

        private void Compress()
        {
            CompressData[] rootInterestingBranchesCompressData = new CompressData[CurrentBranchCount];
            int currentIndex = 0;
            
            TraverseFromRoot(data =>
            {
                if (_nodes[data.nodeIndex].isLeaf)
                {
                    return ExecutionSignal.Continue;
                }

                for (int i = 0; i < currentIndex; i++)
                {
                    if (rootInterestingBranchesCompressData[i].HasNotInterestingNodeWith(data.nodeIndex))
                    {
                        return ExecutionSignal.Continue;
                    }
                }

                rootInterestingBranchesCompressData[currentIndex++] = GetCompressDataFor(data.nodeIndex);
                return ExecutionSignal.Continue;
            });

            for (int i = 0; i < currentIndex; i++)
            {
                Compress(rootInterestingBranchesCompressData[i]);
            }
        }
        
        private void Compress(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            if (_nodes[nodeIndex].isLeaf)
            {
                return;
            }

            Compress(GetCompressDataFor(nodeIndex));
        }

        private void Compress(CompressData data)
        {
            Assert.IsNotNull(data.notInterestingNodeIndexes);
            
            if (data.notInterestingNodeIndexes.Length == 0)
            {
                return;
            }
            
            for (int i = 0; i < data.notInterestingNodeIndexes.Length; i++)
            {
                DeleteChildrenNodesFor(data.notInterestingNodeIndexes[i]);
            }

            Assert.IsTrue(data.firstInterestingNodeIndex != data.nodeIndex);
            LinkChildrenNodesTo(data.nodeIndex, data.firstInterestingNodeIndex);
        }

        // todo: refactoring
        private CompressData GetCompressDataFor(int branchIndex)
        {
            Assert.IsTrue(branchIndex >= 0 && branchIndex < _nodes.Capacity);
            Assert.IsFalse(_nodes[branchIndex].isLeaf);

            int[] notInterestingNodeIndexes = new int[CurrentBranchCount];
            int currentNotInterestingNodeIndex = 0;

            int lastBranchIndex = branchIndex, firstInterestingNodeIndex = branchIndex;
            bool isNotInterestingNode;

            do
            {
                int firstChildIndex = _nodes[lastBranchIndex].firstChildIndex, parentBranchIndex = NULL;
                int branchAmongChildrenCount = 0, childrenObjectCount = 0;

                for (int i = 0; i < 4; i++)
                {
                    int childIndex = firstChildIndex + i;

                    if (_nodes[childIndex].isLeaf == false)
                    {
                        parentBranchIndex = lastBranchIndex;
                        lastBranchIndex = childIndex;
                        branchAmongChildrenCount++;
                    }

                    childrenObjectCount += _nodes[childIndex].objectsCount;
                }

                isNotInterestingNode = childrenObjectCount == 0 && branchAmongChildrenCount == 1;

                if (isNotInterestingNode)
                {
                    Assert.IsTrue(parentBranchIndex != NULL);
                    notInterestingNodeIndexes[currentNotInterestingNodeIndex++] = parentBranchIndex;
                }
                else
                {
                    firstInterestingNodeIndex = firstChildIndex;
                }
            } while (isNotInterestingNode);

            int[] temp = notInterestingNodeIndexes;
            notInterestingNodeIndexes = new int[currentNotInterestingNodeIndex];

            for (int i = 0; i < notInterestingNodeIndexes.Length; i++)
            {
                notInterestingNodeIndexes[i] = temp[(notInterestingNodeIndexes.Length - 1) - i];
            }
            
            return new CompressData
            {
                nodeIndex = branchIndex,
                firstInterestingNodeIndex = firstInterestingNodeIndex,
                notInterestingNodeIndexes = notInterestingNodeIndexes
            };
        }

        private int Decompress(int nodeIndex, AABB2D objBounds)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            int lastBranchIndex = nodeIndex, targetLeafIndex = NULL;
            AABB2D firstChildNodeBounds = _nodes[_nodes[nodeIndex].firstChildIndex].bounds;
            bool isChildFoundForFirstChildNode, isChildFoundForTargetObject;
            
            do
            {
                isChildFoundForFirstChildNode = isChildFoundForTargetObject = false;
                int[] childrenIndexes = Split(lastBranchIndex);
                
                for (int i = 0; i < childrenIndexes.Length; i++)
                {
                    if (_nodes[childrenIndexes[i]].bounds.Contains(firstChildNodeBounds))
                    {
                        LinkChildrenNodesTo(childrenIndexes[i], _nodes[lastBranchIndex].firstChildIndex);
                        LinkChildrenNodesTo(lastBranchIndex, childrenIndexes[0]);
                        
                        lastBranchIndex = childrenIndexes[i];
                        isChildFoundForFirstChildNode = true;
                    }

                    if (_nodes[childrenIndexes[i]].bounds.Intersects(objBounds) && lastBranchIndex != childrenIndexes[i])
                    {
                        targetLeafIndex = childrenIndexes[i];
                        isChildFoundForTargetObject = true;
                    }

                    if (isChildFoundForFirstChildNode && isChildFoundForTargetObject)
                    {
                        break;
                    }
                }
            }
            while (_nodes[lastBranchIndex].isLeaf == false && targetLeafIndex == NULL);
            
            Assert.IsTrue(targetLeafIndex != NULL);
            return targetLeafIndex;
        }
        
        private void LinkObjectPointerTo(int leafIndex, int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

            int objectIndex = _objectPointers[objectPointerIndex].objectIndex;
            NodeObject<TObject> obj = _objects[objectIndex];
            
            obj.leafIndex = leafIndex;
            _objects[objectIndex] = obj;
            
            Node node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                node.firstChildIndex = objectPointerIndex;
                node.objectsCount++;
                
                _nodes[leafIndex] = node;
                return;
            }

            int currentPointerIndex = node.firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            while (currentPointer.nextObjectPointerIndex != NULL)
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

            Node node = _nodes[leafIndex];

            if (node.objectsCount == 0)
            {
                return null;
            }

            int[] unlinkedObjects = new int[node.objectsCount];
            int unlinkedObjectsIndex = 0;

            int currentPointerIndex = node.firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            unlinkedObjects[unlinkedObjectsIndex++] = currentPointerIndex;

            while (currentPointer.nextObjectPointerIndex != NULL)
            {
                int nextObjectPointerIndex = currentPointer.nextObjectPointerIndex;

                currentPointer.nextObjectPointerIndex = NULL;
                _objectPointers[currentPointerIndex] = currentPointer;

                currentPointerIndex = nextObjectPointerIndex;
                currentPointer = _objectPointers[currentPointerIndex];
                
                unlinkedObjects[unlinkedObjectsIndex++] = currentPointerIndex;
            }

            node.firstChildIndex = NULL;
            node.objectsCount = 0;
            _nodes[leafIndex] = node;

            return unlinkedObjects;
        }

        private void LinkChildrenNodesTo(int leafIndex, int firstChildIndex)
        {
            Assert.IsTrue(firstChildIndex >= 0 && firstChildIndex < _nodes.Capacity);
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);

            Node node = _nodes[leafIndex];

            node.isLeaf = false;
            node.firstChildIndex = firstChildIndex;

            _nodes[leafIndex] = node;
        }
        
        private void DeleteChildrenNodesFor(int branchIndex)
        {
            Assert.IsTrue(branchIndex >= 0 && branchIndex < _nodes.Capacity);
            Assert.IsFalse(_nodes[branchIndex].isLeaf);

            Node node = _nodes[branchIndex];
            
            for (int i = 0; i < 4; i++)
            {
                _nodes.RemoveAt(node.firstChildIndex + i);
            }

            node.firstChildIndex = NULL;
            node.isLeaf = true;

            _nodes[branchIndex] = node;
        }

        private int CreateObjectFor(int leafIndex, TObject obj, AABB2D objBounds)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);
            
            _objects.Add(new NodeObject<TObject>
            {
                leafIndex = leafIndex,
                target = obj,
                bounds = objBounds
            }, out int newObjectIndex);
            
            _objectPointers.Add(new ObjectPointer
            {
                objectIndex = newObjectIndex,
                nextObjectPointerIndex = NULL
            }, out int newObjectPointerIndex);

            LinkObjectPointerTo(leafIndex, newObjectPointerIndex);
            
            return newObjectIndex;
        }
        
        private void DeleteObject(int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            
            _objects.RemoveAt(_objectPointers[objectPointerIndex].objectIndex);
            _objectPointers.RemoveAt(objectPointerIndex);
        }

        private int FindTargetNodeIndex(int[] nodeIndexes, AABB2D objBounds)
        {
            Assert.IsNotNull(nodeIndexes);
            
            for (int i = 0; i < nodeIndexes.Length; i++)
            {
                if (_nodes[nodeIndexes[i]].bounds.Intersects(objBounds))
                {
                    return nodeIndexes[i];
                }
            }

            return NULL;
        }

        private int FindTargetNodeIndex(AABB2D objBounds)
        {
            int targetNodeIndex = NULL;

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
        
        private void TraverseFromRoot(Func<TraverseData, ExecutionSignal> eachNodeAction)
        {
            TraverseFrom(_rootIndex, eachNodeAction);
        }

        private static int[] CreateEmptyIndexesArrayByLevel(int capacity)
        {
            int[] emptyIndexes = new int[capacity];

            for (int i = 0; i < emptyIndexes.Length; i++)
            {
                emptyIndexes[i] = NULL;
            }

            return emptyIndexes;
        }
        
        private static AABB2D GetBoundsFor(AABB2D parentBounds, QuadrantNumber quadrantNumber)
        {
            Vector2 extents = 0.5f * parentBounds.Extents;
            Vector2 center = Vector2.zero;
            
            switch (quadrantNumber)
            {
                case QuadrantNumber.One:
                    center = parentBounds.Center + new Vector2(extents.x, extents.y);
                    break;
                case QuadrantNumber.Two:
                    center = parentBounds.Center + new Vector2(-extents.x, extents.y);
                    break;
                case QuadrantNumber.Three:
                    center = parentBounds.Center + new Vector2(-extents.x, -extents.y);
                    break;
                case QuadrantNumber.Four:
                    center = parentBounds.Center + new Vector2(extents.x, -extents.y);
                    break;
            }

            return new AABB2D(center, extents);
        }
    }
}
