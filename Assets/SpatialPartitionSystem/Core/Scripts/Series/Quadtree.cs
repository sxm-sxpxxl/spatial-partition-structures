using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class Quadtree<TObject> where TObject : class
    {
        private const sbyte NULL = -1;
        private const sbyte MAX_POSSIBLE_DEPTH = 8;

        private readonly FreeList<Node> _nodes;
        private readonly FreeList<ObjectPointer> _objectPointers;
        private readonly FreeList<NodeObject<TObject>> _objects;

        private readonly sbyte _maxLeafObjects, _maxDepth;
        private readonly int _rootIndex;
        
        private bool[] _missingObjects;

        private enum QuadrantNumber
        {
            One = 0,
            Two = 1,
            Three = 2,
            Four = 3
        }

        public Quadtree(AABB2D bounds, sbyte maxLeafObjects, sbyte maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = (sbyte) Mathf.Clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);

            int maxNodesCount = (int) Mathf.Pow(4, _maxDepth);
        
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);
            _missingObjects = new bool[initialObjectsCapacity];

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
            Traverse(childIndex =>
            {
                var busyColor = _nodes[childIndex].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                var worldCenter = relativeTransform.TransformPoint(_nodes[childIndex].bounds.Center);
                var worldSize = relativeTransform.TransformPoint(_nodes[childIndex].bounds.Size);

                drawer.DrawWireCube(worldCenter, worldSize);
            });
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int[] allLeavesIndexes = FindAllLeavesIndexes();
            int targetLeafIndex = FindTargetLeafIndex(allLeavesIndexes, objBounds);
            
            if (targetLeafIndex == NULL)
            {
                objectIndex = NULL;
                return false;
            }

            objectIndex = Add(targetLeafIndex, obj, objBounds);
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
            
            Remove(linkedLeafIndex);
            return true;
        }

        public void CleanUp()
        {
            int branchCount = (int) (_nodes.Count / 4);
            
            if (branchCount == 0)
            {
                return;
            }
            
            int[] branchIndexes = new int[branchCount];
            branchIndexes[0] = _rootIndex;
            
            int parentIndex, firstChildIndex, childIndex, childrenObjectsCount;
            bool isCleanUpPossible, hasBranchAmonthChildren;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < branchCount; traverseBranchIndex++)
            {
                parentIndex = branchIndexes[traverseBranchIndex];
                firstChildIndex = _nodes[parentIndex].firstChildIndex;
            
                childrenObjectsCount = 0;
                isCleanUpPossible = false;
                hasBranchAmonthChildren = false;
                
                for (byte childOffset = 0; childOffset < 4; childOffset++)
                {
                    childIndex = firstChildIndex + childOffset;
            
                    if (_nodes[childIndex].isLeaf == false)
                    {
                        branchIndexes[freeBranchIndex++] = childIndex;
                        hasBranchAmonthChildren = true;
                        
                        continue;
                    }
                    
                    childrenObjectsCount += _nodes[childIndex].objectsCount;
                    isCleanUpPossible = hasBranchAmonthChildren == false && childOffset == 3 && childrenObjectsCount <= _maxLeafObjects;
                }
            
                if (isCleanUpPossible == false)
                {
                    continue;
                }
            
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
                
                DeleteChildrenNodesFor(parentIndex);
                
                for (int i = 0; i < childrenUnlinkedPointerIndexes.Length; i++)
                {
                    LinkObjectPointerTo(parentIndex, childrenUnlinkedPointerIndexes[i]);
                }
            }
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

            int[] childrenIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                _nodes.Add(new Node
                {
                    firstChildIndex = NULL,
                    objectsCount = 0,
                    isLeaf = true,
                    depth = (sbyte) (_nodes[leafIndex].depth + 1),
                    bounds = GetBoundsFor(_nodes[leafIndex].bounds, (QuadrantNumber) i)
                }, out childrenIndexes[i]);
            }

            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);
            int unlinkedObjectIndex, targetChildIndex;

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                unlinkedObjectIndex = _objectPointers[unlinkedPointersIndexes[i]].objectIndex;
                targetChildIndex = FindTargetLeafIndex(childrenIndexes, _objects[unlinkedObjectIndex].bounds);

                LinkObjectPointerTo(targetChildIndex, unlinkedPointersIndexes[i]);
            }

            targetChildIndex = FindTargetLeafIndex(childrenIndexes, objBounds);
            
            if (targetChildIndex == NULL)
            {
                return NULL;
            }
            
            int objectIndex = Add(targetChildIndex, obj, objBounds);
            LinkChildrenNodesTo(leafIndex, childrenIndexes[0]);

            return objectIndex;
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
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

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

        private int FindTargetLeafIndex(int[] leavesIndexes, AABB2D objBounds)
        {
            Assert.IsNotNull(leavesIndexes);
            
            for (int i = 0; i < leavesIndexes.Length; i++)
            {
                if (_nodes[leavesIndexes[i]].bounds.Intersects(objBounds))
                {
                    return leavesIndexes[i];
                }
            }

            return NULL;
        }

        private int[] FindAllLeavesIndexes()
        {
            int leavesCount = _nodes.Count - (int) (_nodes.Count / 4);

            int[] allLeaves = new int[leavesCount];
            int leafIndex = 0;

            if (leavesCount == 1)
            {
                allLeaves[0] = _rootIndex;
                return allLeaves;
            }

            Traverse(childIndex =>
            {
                if (_nodes[childIndex].isLeaf == false)
                {
                    return;
                }

                allLeaves[leafIndex++] = childIndex;
            });
            
            return allLeaves;
        }

        private void Traverse(Action<int> eachNodeAction)
        {
            int branchCount = (int) (_nodes.Count / 4);

            if (branchCount == 0)
            {
                eachNodeAction.Invoke(_rootIndex);
                return;
            }
            
            int[] branchesIndexes = new int[branchCount];
            branchesIndexes[0] = _rootIndex;
            
            int parentIndex, firstChildIndex, childIndex;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < branchCount; traverseBranchIndex++)
            {
                parentIndex = branchesIndexes[traverseBranchIndex];
                firstChildIndex = _nodes[parentIndex].firstChildIndex;
                
                for (sbyte childOffset = 0; childOffset < 4; childOffset++)
                {
                    childIndex = firstChildIndex + childOffset;

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
