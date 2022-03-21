using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class CompressedQuadtree<TObject> where TObject : class
    {
        internal const int RootIndex = 0, Null = -1;
        
        private const int MaxPossibleDepth = 8;

        private static int _cachedEqualNodeIndex;
        private static AABB2D _cachedEqualBounds;
        private static List<int> _cachedLeafIndexes = new List<int>(capacity: 1000);
        
        private readonly FreeList<Node> _nodes;
        private readonly FreeList<ObjectPointer> _objectPointers;
        private readonly FreeList<NodeObject<TObject>> _objects;

        private readonly int _maxLeafObjects, _maxDepth;

        private readonly int[] _branchIndexes;
        private MissingObjectData[] _missingObjects;
        private readonly List<TObject> _queryObjects;

        internal int NodesCapacity => _nodes.Capacity;
        
        private int CurrentBranchCount => _nodes.Count / 4;

        private struct MissingObjectData
        {
            public bool isMissing;
            public TObject obj;
        }
        
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
            public Node node;
            
            public bool isParentChanged;
            public bool isLastChild;

            public TraverseData(int nodeIndex, Node node, bool isParentChanged, bool isLastChild)
            {
                this.nodeIndex = nodeIndex;
                this.node = node;
                this.isParentChanged = isParentChanged;
                this.isLastChild = isLastChild;
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

        public CompressedQuadtree(AABB2D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = Mathf.Min(maxDepth, MaxPossibleDepth);

            int maxNodesCount = GetMaxNodesCountFor(_maxDepth);
            int maxBranchCount = maxNodesCount / 4;
        
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);
            _branchIndexes = ArrayUtility.CreateArray(capacity: maxBranchCount, defaultValue: Null);
            _missingObjects = new MissingObjectData[initialObjectsCapacity];
            _queryObjects = new List<TObject>(capacity: initialObjectsCapacity);
            
            _nodes.Add(new Node
            {
                parentIndex = Null,
                firstChildIndex = Null,
                objectsCount = 0,
                isLeaf = true,
                depth = 0,
                bounds = bounds
            }, out _);
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
            if (targetNodeIndex == Null)
            {
                objectIndex = Null;
                return false;
            }
            
            AddToNodeWith(targetNodeIndex, obj, objBounds, out objectIndex);
            return true;
        }

        internal void AddToNodeWith(int nodeIndex, TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsTrue(_nodes.Contains(nodeIndex));
            Assert.IsNotNull(obj);
            
            if (_nodes[nodeIndex].isLeaf)
            {
                objectIndex = AddToLeafWith(nodeIndex, obj, objBounds);
                Compress(nodeIndex);
            }
            else
            {
                int decompressedTargetLeafIndex = Decompress(nodeIndex, objBounds);
                objectIndex = AddToLeafWith(decompressedTargetLeafIndex, obj, objBounds);
                Compress(nodeIndex);
            }
        }

        public bool TryRemove(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsFalse(linkedLeafIndex == Null);
            
            Remove(objectIndex);
            return true;
        }

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
                
                int currentParentIndex = Null, childrenObjectsCount = 0;
                bool hasBranchAmongChildren = false;
                Node targetNode;
                
                TraverseFromRoot(data =>
                {
                    targetNode = _nodes[data.nodeIndex];
                    
                    if (currentParentIndex != targetNode.parentIndex)
                    {
                        currentParentIndex = targetNode.parentIndex;
                        childrenObjectsCount = 0;
                        hasBranchAmongChildren = false;
                    }
                    
                    if (hasBranchAmongChildren == false && targetNode.isLeaf)
                    {
                        childrenObjectsCount += targetNode.objectsCount;
                    }
                    else
                    {
                        hasBranchAmongChildren = true;
                    }

                    if (data.isLastChild && hasBranchAmongChildren == false && childrenObjectsCount <= _maxLeafObjects)
                    {
                        CleanUp(currentParentIndex, childrenObjectsCount);
                        isNeedAnotherCleanUp = true;
                    }
                
                    return ExecutionSignal.Continue;
                }, needTraverseForStartNode: false);
            } while (isNeedAnotherCleanUp);
        }
        
        public int Update(int objectIndex, AABB2D updatedObjBounds)
        {
            int newObjectIndex;
            
            if (IsObjectMissing(objectIndex))
            {
                if (TryAdd(_missingObjects[objectIndex].obj, updatedObjBounds, out newObjectIndex) == false)
                {
                    return objectIndex;
                }
                
                _missingObjects[objectIndex].isMissing = false;
                _missingObjects[objectIndex].obj = null;
                
                return newObjectIndex;
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);

            if (_nodes[linkedLeafIndex].bounds.Intersects(updatedObjBounds))
            {
                NodeObject<TObject> nodeObject = _objects[objectIndex];
                nodeObject.bounds = updatedObjBounds;
                
                _objects[objectIndex] = nodeObject;
                return objectIndex;
            }

            TObject obj = _objects[objectIndex].target;
            Remove(objectIndex);

            if (TryAdd(obj, updatedObjBounds, out newObjectIndex) == false)
            {
                _missingObjects[objectIndex].isMissing = true;
                _missingObjects[objectIndex].obj = obj;
                
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

                AddIntersectedQueryBoundsLeafObjects(data.nodeIndex, queryBounds, _queryObjects);
                return ExecutionSignal.Continue;
            });

            return _queryObjects;
        }

        internal int GetEqualNodeIndexFrom(int nodeIndex, AABB2D equalBounds)
        {
            _cachedEqualNodeIndex = Null;
            _cachedEqualBounds = equalBounds;
            
            TraverseFrom(nodeIndex, data =>
            {
                if (data.node.bounds == _cachedEqualBounds)
                {
                    _cachedEqualNodeIndex = data.nodeIndex;
                    return CompressedQuadtree<TObject>.ExecutionSignal.Stop;
                }

                return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
            });
            
            return _cachedEqualNodeIndex;
        }

        internal void TraverseFrom(int nodeIndex, Func<TraverseData, ExecutionSignal> eachNodeAction, bool needTraverseForStartNode = true)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsNotNull(eachNodeAction);
            
            if (_nodes[nodeIndex].isLeaf)
            {
                if (needTraverseForStartNode)
                {
                    eachNodeAction.Invoke(new TraverseData(nodeIndex, _nodes[nodeIndex], false, false));   
                }
                return;
            }
            
            _branchIndexes[0] = nodeIndex;

            ExecutionSignal executionSignal;
            if (needTraverseForStartNode)
            {
                executionSignal = eachNodeAction.Invoke(new TraverseData(nodeIndex, _nodes[nodeIndex], false, false));
                if (executionSignal == ExecutionSignal.Stop)
                {
                    return;
                }
            }

            int parentIndex, firstChildIndex, childIndex;
            Node childNode;
            bool isParentChanged;

            for (int traverseBranchIndex = 0, freeBranchIndex = 1;
                traverseBranchIndex < _branchIndexes.Length && _branchIndexes[traverseBranchIndex] != Null;
                traverseBranchIndex++)
            {
                parentIndex = _branchIndexes[traverseBranchIndex];
                isParentChanged = true;
                firstChildIndex = _nodes[parentIndex].firstChildIndex;

                for (int i = 0; i < 4; i++)
                {
                    childIndex = firstChildIndex + i;
                    childNode = _nodes[childIndex];
                    
                    executionSignal = eachNodeAction.Invoke(new TraverseData(childIndex, childNode, isParentChanged, i == 3));
                    if (executionSignal == ExecutionSignal.Stop)
                    {
                        ClearBranchIndexes(fromIndex: traverseBranchIndex, toIndex: freeBranchIndex);
                        return;
                    }

                    isParentChanged = false;

                    if (childNode.isLeaf == false)
                    {
                        _branchIndexes[freeBranchIndex++] = childIndex;
                    }
                    
                    if (executionSignal == ExecutionSignal.ContinueInDepth)
                    {
                        ClearBranchIndexes(fromIndex: traverseBranchIndex, toIndex: freeBranchIndex);
                        freeBranchIndex = traverseBranchIndex + 1;
                        
                        if (childNode.isLeaf == false)
                        {
                            _branchIndexes[freeBranchIndex++] = childIndex;
                        }
                        
                        break;
                    }
                }

                _branchIndexes[traverseBranchIndex] = Null;
            }
        }

        internal Node GetNodeBy(int nodeIndex) => _nodes[nodeIndex];

        internal NodeObject<TObject> GetNodeObjectBy(int objectIndex) => _objects[objectIndex];

        internal bool ContainsNodeWith(int nodeIndex) => _nodes.Contains(nodeIndex);

        internal bool ContainsObjectWith(int objectIndex) => _objects.Contains(objectIndex);

        internal void AddIntersectedQueryBoundsLeafObjects(int leafIndex, AABB2D queryBounds, ICollection<TObject> objects)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            
            Node leaf = _nodes[leafIndex];
            Assert.IsTrue(leaf.isLeaf);

            if (leaf.objectsCount == 0)
            {
                return;
            }

            ObjectPointer currentPointer;
            NodeObject<TObject> obj;
            int currentPointerIndex = leaf.firstChildIndex;
            
            do
            {
                currentPointer = _objectPointers[currentPointerIndex];
                obj = _objects[currentPointer.objectIndex];
                
                if (queryBounds.Intersects(obj.bounds))
                {
                    objects.Add(obj.target);
                }
                
                currentPointerIndex = currentPointer.nextObjectPointerIndex;
            }
            while (currentPointer.nextObjectPointerIndex != Null);
        }

        internal void DeepAddNodeObjects(int nodeIndex, ICollection<TObject> objects)
        {
            if (_nodes[nodeIndex].isLeaf)
            {
                AddLeafObjects(nodeIndex, objects);
                return;
            }
            
            TraverseFrom(nodeIndex, data =>
            {
                if (data.node.isLeaf)
                {
                    _cachedLeafIndexes.Add(data.nodeIndex);
                }
                return ExecutionSignal.Continue;
            }, needTraverseForStartNode: false);
            
            for (int i = 0; i < _cachedLeafIndexes.Count; i++)
            {
                AddLeafObjects(_cachedLeafIndexes[i], objects);
            }
            
            _cachedLeafIndexes.Clear();
        }

        private void ClearBranchIndexes(int fromIndex, int toIndex)
        {
            for (int i = fromIndex; i < toIndex && _branchIndexes[i] != Null; i++)
            {
                _branchIndexes[i] = Null;
            }
        }

        private bool IsObjectMissing(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            if (objectIndex >= _missingObjects.Length)
            {
                var newArray = new MissingObjectData[_objects.Capacity];
                _missingObjects.CopyTo(newArray, 0);
                _missingObjects = newArray;
            }

            return _missingObjects[objectIndex].isMissing;
        }

        private void AddLeafObjects(int leafIndex, ICollection<TObject> objects)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            
            Node leaf = _nodes[leafIndex];
            Assert.IsTrue(leaf.isLeaf);

            if (leaf.objectsCount == 0)
            {
                return;
            }

            ObjectPointer currentPointer;
            NodeObject<TObject> obj;
            int currentPointerIndex = leaf.firstChildIndex;
            
            do
            {
                currentPointer = _objectPointers[currentPointerIndex];
                obj = _objects[currentPointer.objectIndex];
                
                objects.Add(obj.target);

                currentPointerIndex = currentPointer.nextObjectPointerIndex;
            }
            while (currentPointer.nextObjectPointerIndex != Null);
        }

        private int AddToLeafWith(int leafIndex, TObject obj, AABB2D objBounds)
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
            
            if (targetChildIndex == Null)
            {
                return Null;
            }
            
            int objectIndex = AddToLeafWith(targetChildIndex, obj, objBounds);
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
                    parentIndex = Null,
                    firstChildIndex = Null,
                    objectsCount = 0,
                    isLeaf = true,
                    depth = (byte) (_nodes[nodeIndex].depth + 1),
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

        private CompressData GetCompressDataFor(int branchIndex)
        {
            Assert.IsTrue(branchIndex >= 0 && branchIndex < _nodes.Capacity);
            Assert.IsFalse(_nodes[branchIndex].isLeaf);

            int[] notInterestingNodeIndexes = new int[CurrentBranchCount];
            int currentNotInterestingNodeIndex = 0;

            int firstInterestingNodeIndex = branchIndex, currentParentIndex = Null, branchAmongChildrenCount = 0, childrenObjectCount = 0;
            bool isNotInterestingNode;

            TraverseFrom(branchIndex, data =>
            {
                if (data.isParentChanged)
                {
                    currentParentIndex = data.node.parentIndex;
                    branchAmongChildrenCount = childrenObjectCount = 0;
                }

                if (data.node.isLeaf == false)
                {
                    branchAmongChildrenCount++;
                }

                childrenObjectCount += data.node.objectsCount;

                if (data.isLastChild)
                {
                    bool isNotInterestingNode = childrenObjectCount == 0 && branchAmongChildrenCount == 1;
                    
                    if (isNotInterestingNode)
                    {
                        Assert.IsTrue(currentParentIndex != Null);
                        notInterestingNodeIndexes[currentNotInterestingNodeIndex++] = currentParentIndex;
                    }
                    else
                    {
                        firstInterestingNodeIndex = _nodes[currentParentIndex].firstChildIndex;
                        return ExecutionSignal.Stop;
                    }
                }

                return ExecutionSignal.Continue;
            }, needTraverseForStartNode: false);
            
            int[] oversizedNotInterestingNodeIndexes = notInterestingNodeIndexes;
            notInterestingNodeIndexes = new int[currentNotInterestingNodeIndex];

            for (int i = 0; i < notInterestingNodeIndexes.Length; i++)
            {
                notInterestingNodeIndexes[i] = oversizedNotInterestingNodeIndexes[(notInterestingNodeIndexes.Length - 1) - i];
            }
            
            return new CompressData
            {
                nodeIndex = branchIndex,
                firstInterestingNodeIndex = firstInterestingNodeIndex,
                notInterestingNodeIndexes = notInterestingNodeIndexes
            };
        }

        private int Decompress(int branchIndex, AABB2D objBounds)
        {
            Assert.IsTrue(branchIndex >= 0 && branchIndex < _nodes.Capacity);
            Assert.IsFalse(_nodes[branchIndex].isLeaf);

            int lastBranchIndex = branchIndex, targetLeafIndex = Null;
            AABB2D firstChildNodeBounds = _nodes[_nodes[branchIndex].firstChildIndex].bounds;
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
            while (_nodes[lastBranchIndex].isLeaf == false && targetLeafIndex == Null);
            
            Assert.IsFalse(targetLeafIndex == Null);
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

        private void LinkChildrenNodesTo(int nodeIndex, int firstChildIndex)
        {
            Assert.IsTrue(firstChildIndex >= 0 && firstChildIndex < _nodes.Capacity);
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            Node node = _nodes[nodeIndex];

            node.isLeaf = false;
            node.firstChildIndex = firstChildIndex;

            _nodes[nodeIndex] = node;
            
            for (int i = 0; i < 4; i++)
            {
                node = _nodes[firstChildIndex + i];
                node.parentIndex = nodeIndex;
                _nodes[firstChildIndex + i] = node;
            }
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

            node.firstChildIndex = Null;
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
                nextObjectPointerIndex = Null
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

            return Null;
        }

        private int FindTargetNodeIndex(AABB2D objBounds)
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
        
        private void TraverseFromRoot(Func<TraverseData, ExecutionSignal> eachNodeAction, bool needTraverseForStartNode = true)
        {
            Assert.IsNotNull(eachNodeAction);
            TraverseFrom(RootIndex, eachNodeAction, needTraverseForStartNode);
        }

        private static int GetMaxNodesCountFor(int maxDepth)
        {
            int maxNodesCount = 0;

            for (int i = 0; i <= maxDepth; i++)
            {
                maxNodesCount += (int) Mathf.Pow(4, i);
            }
            
            return maxNodesCount;
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
