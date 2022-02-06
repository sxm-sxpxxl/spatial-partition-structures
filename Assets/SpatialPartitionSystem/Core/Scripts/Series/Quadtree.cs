using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEditor.UIElements;
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
        private readonly AABB2D _rootBounds;

        private enum QuadrantNumber
        {
            One = 0,
            Two = 1,
            Three = 2,
            Four = 3
        }

        private struct NodeWithBounds
        {
            public int index;
            public sbyte depth;

            public AABB2D bounds;
        }

        public Quadtree(AABB2D bounds, sbyte maxLeafObjects, sbyte maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = (sbyte) Mathf.Clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);

            int maxNodesCount = (int) Mathf.Pow(4, _maxDepth);
            
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);

            _rootBounds = bounds;
            var root = new Node {firstChildIndex = NULL, objectsCount = 0, isLeaf = true};
            _nodes.Add(root, out _rootIndex);
        }

        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();
            
            Traverse(childData =>
            {
                var busyColor = _nodes[childData.index].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                var worldCenter = relativeTransform.TransformPoint(childData.bounds.Center);
                var worldSize = relativeTransform.TransformPoint(childData.bounds.Size);

                drawer.DrawWireCube(worldCenter, worldSize);
            });
        }

        public bool TryAdd(TObject obj, AABB2D objBounds)
        {
            Assert.IsNotNull(obj);
            
            NodeWithBounds[] allLeaves = FindAllLeaves();
            if (FindTargetLeaf(allLeaves, objBounds, out NodeWithBounds targetLeaf) == false)
            {
                return false;
            }

            Add(targetLeaf, new NodeObject<TObject> { target = obj, bounds = objBounds });
            return true;
        }

        public bool TryRemove(TObject obj, AABB2D objBounds)
        {
            Assert.IsNotNull(obj);
            
            NodeWithBounds[] allLeaves = FindAllLeaves();
            int targetLeafIndex = FindTargetLeafIndex(allLeaves, objBounds);

            if (targetLeafIndex == NULL)
            {
                return false;
            }

            Remove(targetLeafIndex, obj);
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

        public void Update(TObject obj, AABB2D objBounds)
        {
            Assert.IsNotNull(obj);
            
            NodeWithBounds[] allLeaves = FindAllLeaves();

            if (FindLinkedLeaf(allLeaves, obj, out NodeWithBounds linkedLeaf))
            {
                if (linkedLeaf.bounds.Intersects(objBounds))
                {
                    return;
                }
                
                Remove(linkedLeaf.index, obj);
            }

            if (FindTargetLeaf(allLeaves, objBounds, out NodeWithBounds targetLeaf) == false)
            {
                return;
            }
            
            Add(targetLeaf, new NodeObject<TObject> { target = obj, bounds = objBounds });
        }

        private void Add(NodeWithBounds leaf, NodeObject<TObject> obj)
        {
            Assert.IsTrue(leaf.index >= 0 && leaf.index < _nodes.Capacity);
            Assert.IsTrue(_nodes[leaf.index].isLeaf);
            Assert.IsNotNull(obj.target);
            
            if (leaf.depth >= _maxDepth || _nodes[leaf.index].objectsCount < _maxLeafObjects)
            {
                CreateObjectFor(leaf.index, obj);
            }
            else
            {
                Split(leaf, obj);
            }
        }
        
        private void Remove(int leafIndex, TObject obj)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);
            
            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);
            ObjectPointer unlinkedObjectPointer;
            int nextObjectPointerIndex;

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                unlinkedObjectPointer = _objectPointers[unlinkedPointersIndexes[i]];

                if (_objects[unlinkedObjectPointer.objectIndex].target == obj)
                {
                    nextObjectPointerIndex = unlinkedObjectPointer.nextObjectPointerIndex;
                    DeleteObject(unlinkedPointersIndexes[i]);
                }
                else
                {
                    LinkObjectPointerTo(leafIndex, unlinkedPointersIndexes[i]);
                }
            }
        }

        private void Split(NodeWithBounds leaf, NodeObject<TObject> obj)
        {
            Assert.IsTrue(leaf.index >= 0 && leaf.index < _nodes.Capacity);
            Assert.IsTrue(_nodes[leaf.index].isLeaf);
            Assert.IsNotNull(obj.target);
            
            NodeWithBounds[] children = new NodeWithBounds[4];
            for (int i = 0; i < 4; i++)
            {
                var child = new Node {isLeaf = true, objectsCount = 0, firstChildIndex = NULL};
                _nodes.Add(child, out int index);

                children[i] = new NodeWithBounds
                {
                    index = index,
                    depth = (sbyte) (leaf.depth + 1),
                    bounds = GetBoundsFor(leaf.bounds, (QuadrantNumber) i)
                };
            }

            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leaf.index);
            int unlinkedObjectIndex, targetChildIndex;

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                unlinkedObjectIndex = _objectPointers[unlinkedPointersIndexes[i]].objectIndex;
                targetChildIndex = FindTargetLeafIndex(children, _objects[unlinkedObjectIndex].bounds);

                LinkObjectPointerTo(targetChildIndex, unlinkedPointersIndexes[i]);
            }

            if (FindTargetLeaf(children, obj.bounds, out NodeWithBounds targetChild) == false)
            {
                return;
            }
            
            Add(targetChild, obj);
            LinkChildrenNodesTo(leaf.index, children[0].index);
        }

        private void LinkObjectPointerTo(int leafIndex, int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);

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

        private bool IsLinkedWithObjectPointer(int objectPointerIndex, TObject obj)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            Assert.IsNotNull(obj);
            
            int objectIndex = _objectPointers[objectPointerIndex].objectIndex;
            return _objects[objectIndex].target == obj;
        }

        private bool IsLinkedWithLeaf(int leafIndex, TObject obj)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj);

            Node node = _nodes[leafIndex];
            
            if (node.isLeaf == false || node.objectsCount == 0)
            {
                return false;
            }
            
            int currentPointerIndex = node.firstChildIndex;
            ObjectPointer currentPointer = _objectPointers[currentPointerIndex];

            if (IsLinkedWithObjectPointer(currentPointerIndex, obj))
            {
                return true;
            }

            while (currentPointer.nextObjectPointerIndex != NULL)
            {
                currentPointerIndex = currentPointer.nextObjectPointerIndex;
                currentPointer = _objectPointers[currentPointerIndex];

                if (IsLinkedWithObjectPointer(currentPointerIndex, obj))
                {
                    return true;
                }
            }
            
            return false;
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

        private void CreateObjectFor(int leafIndex, NodeObject<TObject> obj)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            Assert.IsNotNull(obj.target);
            
            _objects.Add(obj, out int newObjectIndex);
            
            var newObjectPointer = new ObjectPointer { objectIndex = newObjectIndex, nextObjectPointerIndex = NULL };
            _objectPointers.Add(newObjectPointer, out int newObjectPointerIndex);

            LinkObjectPointerTo(leafIndex, newObjectPointerIndex);
        }
        
        private void DeleteObject(int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            
            _objects.RemoveAt(_objectPointers[objectPointerIndex].objectIndex);
            _objectPointers.RemoveAt(objectPointerIndex);
        }

        private int FindTargetLeafIndex(NodeWithBounds[] leaves, AABB2D objBounds)
        {
            Assert.IsNotNull(leaves);
            
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i].bounds.Intersects(objBounds))
                {
                    return leaves[i].index;
                }
            }

            return NULL;
        }

        private bool FindTargetLeaf(NodeWithBounds[] leaves, AABB2D objBounds, out NodeWithBounds targetLeaf)
        {
            Assert.IsNotNull(leaves);
            
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i].bounds.Intersects(objBounds))
                {
                    targetLeaf = leaves[i];
                    return true;
                }
            }

            targetLeaf = new NodeWithBounds();
            return false;
        }

        private bool FindLinkedLeaf(NodeWithBounds[] leaves, TObject obj, out NodeWithBounds linkedLeaf)
        {
            Assert.IsNotNull(leaves);
            Assert.IsNotNull(obj);
            
            for (int i = 0; i < leaves.Length; i++)
            {
                if (IsLinkedWithLeaf(leaves[i].index, obj))
                {
                    linkedLeaf = leaves[i];
                    return true;
                }
            }
            
            linkedLeaf = new NodeWithBounds();
            return false;
        }
        
        private NodeWithBounds[] FindAllLeaves()
        {
            int leavesCount = _nodes.Count - (int) (_nodes.Count / 4);

            NodeWithBounds[] allLeaves = new NodeWithBounds[leavesCount];
            int leafIndex = 0;

            if (leavesCount == 1)
            {
                allLeaves[0] = new NodeWithBounds { index = _rootIndex, bounds = _rootBounds, depth = 0 };
                return allLeaves;
            }

            Traverse(childData =>
            {
                if (_nodes[childData.index].isLeaf == false)
                {
                    return;
                }
                
                allLeaves[leafIndex++] = new NodeWithBounds
                {
                    index = childData.index,
                    bounds = childData.bounds,
                    depth = childData.depth
                };
            });
            
            return allLeaves;
        }

        private void Traverse(Action<NodeWithBounds> eachNodeAction)
        {
            int branchCount = (int) (_nodes.Count / 4);

            if (branchCount == 0)
            {
                eachNodeAction.Invoke(new NodeWithBounds
                {
                    index = _rootIndex,
                    depth = 0,
                    bounds = _rootBounds
                });
                return;
            }
            
            NodeWithBounds[] branches = new NodeWithBounds[branchCount];
            branches[0] = new NodeWithBounds { index = _rootIndex, bounds = _rootBounds, depth = 0 };
            
            int parentIndex, firstChildIndex, childIndex;
            sbyte childDepth;
            AABB2D parentBounds;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < branchCount; traverseBranchIndex++)
            {
                parentIndex = branches[traverseBranchIndex].index;
                parentBounds = branches[traverseBranchIndex].bounds;
                childDepth = (sbyte) (branches[traverseBranchIndex].depth + 1);
                firstChildIndex = _nodes[parentIndex].firstChildIndex;
                
                for (sbyte childOffset = 0; childOffset < 4; childOffset++)
                {
                    childIndex = firstChildIndex + childOffset;

                    if (_nodes[childIndex].isLeaf == false)
                    {
                        branches[freeBranchIndex++] = new NodeWithBounds
                        {
                            index = childIndex,
                            bounds = GetBoundsFor(parentBounds, (QuadrantNumber) childOffset),
                            depth = childDepth
                        };
                    }

                    eachNodeAction.Invoke(new NodeWithBounds
                    {
                        index = childIndex,
                        depth = childDepth,
                        bounds = GetBoundsFor(parentBounds, (QuadrantNumber) childOffset)
                    });
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