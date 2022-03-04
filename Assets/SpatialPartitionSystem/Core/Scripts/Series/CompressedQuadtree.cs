using System;
using System.Collections.Generic;
using Codice.CM.Common;
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

        private int CurrentBranchCount => (int) (_nodes.Count / 4);

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
            _maxDepth = (sbyte) Mathf.Clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);

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
            Traverse(nodeIndex =>
            {
                var busyColor = _nodes[nodeIndex].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                var worldCenter = relativeTransform.TransformPoint(_nodes[nodeIndex].bounds.Center);
                var worldSize = relativeTransform.TransformPoint(_nodes[nodeIndex].bounds.Size);

                drawer.DrawWireCube(worldCenter, worldSize);
            });
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int targetNodeIndex = FindTargetNodeIndex(objBounds);
            
            if (targetNodeIndex == NULL)
            {
                objectIndex = NULL;
                return false;
            }

            if (_nodes[targetNodeIndex].isLeaf)
            {
                objectIndex = Add(targetNodeIndex, obj, objBounds);
                Compress(targetNodeIndex);
            }
            else
            {
                int decompressedTargetNodeIndex = Decompress(targetNodeIndex, objBounds);
                objectIndex = Add(decompressedTargetNodeIndex, obj, objBounds);
                Compress(targetNodeIndex);
            }
            
            return true;
        }

        // todo: refactoring
        private void Compress(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            if (_nodes[nodeIndex].isLeaf)
            {
                return;
            }

            int[] notInterestingNodeIndexes = new int[CurrentBranchCount];
            int currentNotInterestingNodeIndex = 0;

            int lastBranchIndex = nodeIndex, firstInterestingNodeIndex = nodeIndex;
            bool isNotInterestingNode = false, isNeedCompress = false;

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
                isNeedCompress |= isNotInterestingNode;

                if (isNotInterestingNode)
                {
                    notInterestingNodeIndexes[currentNotInterestingNodeIndex++] = parentBranchIndex;
                }
                else
                {
                    firstInterestingNodeIndex = firstChildIndex;
                }
                
            } while (isNotInterestingNode);
                
            if (isNeedCompress == false)
            {
                return;
            }

            while (--currentNotInterestingNodeIndex >= 0)
            {
                DeleteChildrenNodesFor(notInterestingNodeIndexes[currentNotInterestingNodeIndex]);
            }
            
            LinkChildrenNodesTo(nodeIndex, firstInterestingNodeIndex);
        }

        // todo: refactoring
        private int Decompress(int nodeIndex, AABB2D objBounds)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);

            int lastBranchIndex = nodeIndex, targetLeafIndex = NULL;
            Node firstChildNode = _nodes[_nodes[nodeIndex].firstChildIndex];
            
            do
            {
                int[] childrenIndexes = Split(lastBranchIndex);
                
                for (int i = 0; i < childrenIndexes.Length; i++)
                {
                    if (_nodes[childrenIndexes[i]].bounds.Contains(firstChildNode.bounds))
                    {
                        LinkChildrenNodesTo(childrenIndexes[i], _nodes[lastBranchIndex].firstChildIndex);
                        LinkChildrenNodesTo(lastBranchIndex, childrenIndexes[0]);
                        
                        lastBranchIndex = childrenIndexes[i];
                    }

                    if (_nodes[childrenIndexes[i]].bounds.Contains(objBounds) && lastBranchIndex != childrenIndexes[i])
                    {
                        targetLeafIndex = childrenIndexes[i];
                    }
                }
            }
            while (_nodes[lastBranchIndex].isLeaf == false && targetLeafIndex == NULL);
            
            return targetLeafIndex;
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

        // todo: refactoring
        public void CleanUp()
        {
            if (CurrentBranchCount == 0)
            {
                return;
            }
            
            int[] branchIndexes = new int[CurrentBranchCount];
            branchIndexes[0] = _rootIndex;
            
            int parentBranchIndex, childIndex, childrenObjectsCount;
            bool isCleanUpPossible, hasBranchAmongChildren, isNeedAnotherCleanUp;

            do
            {
                isNeedAnotherCleanUp = false;

                for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < CurrentBranchCount; traverseBranchIndex++)
                {
                    parentBranchIndex = branchIndexes[traverseBranchIndex];

                    childrenObjectsCount = 0;
                    isCleanUpPossible = hasBranchAmongChildren = false;

                    for (int i = 0; i < 4; i++)
                    {
                        childIndex = _nodes[parentBranchIndex].firstChildIndex + i;

                        if (_nodes[childIndex].isLeaf == false)
                        {
                            branchIndexes[freeBranchIndex++] = childIndex;
                            hasBranchAmongChildren = true;

                            continue;
                        }

                        childrenObjectsCount += _nodes[childIndex].objectsCount;
                        isCleanUpPossible = hasBranchAmongChildren == false && i == 3 && childrenObjectsCount <= _maxLeafObjects;
                    }

                    Compress(parentBranchIndex);

                    if (isCleanUpPossible)
                    {
                        CleanUp(parentBranchIndex, childrenObjectsCount);
                        isNeedAnotherCleanUp = true;
                    }
                }
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
            
            Traverse(nodeIndex =>
            {
                if (_nodes[nodeIndex].isLeaf == false || queryBounds.Intersects(_nodes[nodeIndex].bounds) == false)
                {
                    return;
                }
            
                TraverseObjects(nodeIndex, objectIndex =>
                {
                    if (queryBounds.Intersects(_objects[objectIndex].bounds))
                    {
                        _queryObjects.Add(_objects[objectIndex].target);
                    }
                });
            });

            return _queryObjects;
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

            int currentPointerIndex = node.firstChildIndex, nextObjectPointerIndex = NULL;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            unlinkedObjects[unlinkedObjectsIndex++] = currentPointerIndex;

            while (currentPointer.nextObjectPointerIndex != NULL)
            {
                nextObjectPointerIndex = currentPointer.nextObjectPointerIndex;

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
            int[] targetNodeIndex = new int[1] { NULL };

            Traverse(nodeIndex =>
            {
                if (_nodes[nodeIndex].bounds.Intersects(objBounds))
                {
                    targetNodeIndex[0] = nodeIndex;
                }
            });
            
            return targetNodeIndex[0];
        }

        private void Traverse(Action<int> eachNodeAction)
        {
            if (CurrentBranchCount == 0)
            {
                eachNodeAction.Invoke(_rootIndex);
                return;
            }
            
            int[] branchesIndexes = new int[CurrentBranchCount];
            branchesIndexes[0] = _rootIndex;
            
            int parentIndex, firstChildIndex, childIndex;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < CurrentBranchCount; traverseBranchIndex++)
            {
                parentIndex = branchesIndexes[traverseBranchIndex];
                firstChildIndex = _nodes[parentIndex].firstChildIndex;
                
                eachNodeAction.Invoke(parentIndex);

                for (int i = 0; i < 4; i++)
                {
                    childIndex = firstChildIndex + i;

                    if (_nodes[childIndex].isLeaf == false)
                    {
                        branchesIndexes[freeBranchIndex++] = childIndex;
                    }

                    eachNodeAction.Invoke(childIndex);
                }
            }
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
