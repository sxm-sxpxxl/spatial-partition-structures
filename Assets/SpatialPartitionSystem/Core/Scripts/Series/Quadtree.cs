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
        private const int NULL = -1;
        private const int MAX_POSSIBLE_DEPTH = 8;

        private readonly FreeList<Node> _nodes;
        private readonly FreeList<ObjectPointer> _objectPointers;
        private readonly FreeList<NodeObject<TObject>> _objects;

        private readonly int _maxLeafObjects, _maxDepth;

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
            public short depth;

            public AABB2D bounds;
        }

        public Quadtree(AABB2D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = Mathf.Clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);

            // todo: ?
            int maxNodesCount = (int) Mathf.Pow(4, _maxDepth);
            
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);

            _rootBounds = bounds;
            var root = new Node {firstChildIndex = NULL, objectsCount = 0, isLeaf = true};
            _nodes.Add(root, out _rootIndex);
        }

        public bool TryAdd(TObject obj, AABB2D objBounds)
        {
            // + 1) find all leaves
            // + 2) find target leaf among all leaves (*2)
            // + 3) add object to target leaf
            //      IF object fits to target leaf: + 3.1) create object to target leaf (*1)
            //      ELSE: + 3.2) split target leaf
            //              + 3.2.1) create 4 childrens
            //              + 3.2.2) transfer data from parent leaf to children
            //              + 3.2.3) find target leaf among children leaves (*2)
            //              + 3.2.4) create object to target leaf (*1)
            // -----------------------------------------------------------------------------
            // *1 - create object to target leaf FUNCTION (...)
            // *2 - find target leaf among given leaves FUNCTION (...)

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
            NodeWithBounds[] allLeaves = FindAllLeaves();
            int targetLeafIndex = FindTargetLeafIndex(allLeaves, objBounds);

            if (targetLeafIndex == NULL)
            {
                return false;
            }

            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(targetLeafIndex);
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
                    LinkObjectPointerTo(targetLeafIndex, unlinkedPointersIndexes[i]);
                }
            }
            
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
            bool isCleanUpPossible;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < branchCount; traverseBranchIndex++)
            {
                parentIndex = branchIndexes[traverseBranchIndex];
                firstChildIndex = _nodes[parentIndex].firstChildIndex;

                childrenObjectsCount = 0;
                isCleanUpPossible = false;
                
                for (int childOffset = 0; childOffset < 4; childOffset++)
                {
                    childIndex = firstChildIndex + childOffset;

                    if (_nodes[childIndex].isLeaf == false)
                    {
                        branchIndexes[freeBranchIndex++] = childIndex;
                        break;
                    }
                    
                    childrenObjectsCount += _nodes[childIndex].objectsCount;
                    isCleanUpPossible = childOffset == 3 && childrenObjectsCount <= _maxLeafObjects;
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

        private void Add(NodeWithBounds leaf, NodeObject<TObject> obj)
        {
            if (leaf.depth >= _maxDepth || _nodes[leaf.index].objectsCount < _maxLeafObjects)
            {
                CreateObjectFor(leaf.index, obj);
            }
            else
            {
                Split(leaf, obj);
            }
        }

        private void Split(NodeWithBounds leaf, NodeObject<TObject> obj)
        {
            NodeWithBounds[] children = new NodeWithBounds[4];
            for (int i = 0; i < 4; i++)
            {
                var child = new Node {isLeaf = true, objectsCount = 0, firstChildIndex = NULL};
                _nodes.Add(child, out int index);

                children[i] = new NodeWithBounds
                {
                    index = index,
                    depth = (short) (leaf.depth + 1),
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

            FindTargetLeaf(children, obj.bounds, out NodeWithBounds targetChild);
            Add(targetChild, obj);
            
            LinkChildrenNodesTo(leaf.index, children[0].index);
        }

        private int[] UnlinkObjectPointersFrom(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[nodeIndex].isLeaf);

            Node node = _nodes[nodeIndex];

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
            _nodes[nodeIndex] = node;

            return unlinkedObjects;
        }

        private void LinkObjectPointerTo(int nodeIndex, int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[nodeIndex].isLeaf);

            Node node = _nodes[nodeIndex];

            if (node.objectsCount == 0)
            {
                node.firstChildIndex = objectPointerIndex;
                node.objectsCount++;

                _nodes[nodeIndex] = node;
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
            _nodes[nodeIndex] = node;
        }

        private void LinkChildrenNodesTo(int nodeIndex, int firstChildIndex)
        {
            Assert.IsTrue(firstChildIndex >= 0 && firstChildIndex < _nodes.Capacity);
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[nodeIndex].isLeaf);

            Node node = _nodes[nodeIndex];

            node.isLeaf = false;
            node.firstChildIndex = firstChildIndex;

            _nodes[nodeIndex] = node;
        }
        
        private void DeleteChildrenNodesFor(int nodeIndex)
        {
            Assert.IsTrue(nodeIndex >= 0 && nodeIndex < _nodes.Capacity);
            Assert.IsFalse(_nodes[nodeIndex].isLeaf);

            Node node = _nodes[nodeIndex];
            
            for (int i = 0; i < 4; i++)
            {
                _nodes.RemoveAt(node.firstChildIndex + i);
            }

            node.firstChildIndex = NULL;
            node.isLeaf = true;

            _nodes[nodeIndex] = node;
        }

        private void CreateObjectFor(int nodeIndex, NodeObject<TObject> obj)
        {
            _objects.Add(obj, out int newObjectIndex);
            
            var newObjectPointer = new ObjectPointer { objectIndex = newObjectIndex, nextObjectPointerIndex = NULL };
            _objectPointers.Add(newObjectPointer, out int newObjectPointerIndex);

            LinkObjectPointerTo(nodeIndex, newObjectPointerIndex);
        }
        
        private void DeleteObject(int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            
            _objects.RemoveAt(_objectPointers[objectPointerIndex].objectIndex);
            _objectPointers.RemoveAt(objectPointerIndex);
        }

        private int FindTargetLeafIndex(NodeWithBounds[] leaves, AABB2D objBounds)
        {
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i].bounds.Contains(objBounds))
                {
                    return leaves[i].index;
                }
            }

            return NULL;
        }

        private bool FindTargetLeaf(NodeWithBounds[] leaves, AABB2D objBounds, out NodeWithBounds targetLeaf)
        {
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i].bounds.Contains(objBounds))
                {
                    targetLeaf = leaves[i];
                    return true;
                }
            }

            targetLeaf = new NodeWithBounds();
            return false;
        }
        
        private NodeWithBounds[] FindAllLeaves()
        {
            int branchCount = (int) (_nodes.Count / 4);
            int leavesCount = _nodes.Count - branchCount;

            NodeWithBounds[] allLeaves = new NodeWithBounds[leavesCount];
            int leafIndex = 0;

            if (leavesCount == 1)
            {
                allLeaves[0] = new NodeWithBounds { index = _rootIndex, bounds = _rootBounds, depth = 0 };
                return allLeaves;
            }

            NodeWithBounds[] branches = new NodeWithBounds[branchCount];
            branches[0] = new NodeWithBounds { index = _rootIndex, bounds = _rootBounds, depth = 0 };
            
            int parentIndex, firstChildIndex, childIndex;
            short childDepth;
            AABB2D parentBounds;
            
            for (int traverseBranchIndex = 0, freeBranchIndex = 1; traverseBranchIndex < branchCount; traverseBranchIndex++)
            {
                parentIndex = branches[traverseBranchIndex].index;
                parentBounds = branches[traverseBranchIndex].bounds;
                childDepth = (short) (branches[traverseBranchIndex].depth + 1);
                firstChildIndex = _nodes[parentIndex].firstChildIndex;

                for (int childOffset = 0; childOffset < 4; childOffset++)
                {
                    childIndex = firstChildIndex + childOffset;

                    if (_nodes[childIndex].isLeaf)
                    {
                        allLeaves[leafIndex++] = new NodeWithBounds
                        {
                            index = childIndex,
                            bounds = GetBoundsFor(parentBounds, (QuadrantNumber) childOffset),
                            depth = childDepth
                        };
                    }
                    else
                    {
                        branches[freeBranchIndex++] = new NodeWithBounds
                        {
                            index = childIndex,
                            bounds = GetBoundsFor(parentBounds, (QuadrantNumber) childOffset),
                            depth = childDepth
                        };
                    }
                }
            }
            
            // all count | leaves count |
            //      1           1
            //      5           4
            //      9           7
            //      13          10
            //      17          13
            // ---------------------------
            // leaves count = all count - (int) (all count / 4)
            
            return allLeaves;
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