using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class SkipQuadtree<TObject> where TObject : class
    {
        private const float DefaultExtendedEpsilon = 0.1f;
        private const int Null = CompressedQuadtree<TObject>.Null, MaxPossibleLevelsQuantity = 8;
        
        private readonly CompressedQuadtree<TObject>[] _levelTrees;
        private readonly Dictionary<int, NodeCopyPointer>[] _nodeCopyPointersByLevel;
        private readonly Dictionary<TObject, int[]> _levelTreesObjectIndexes;
        
        private readonly List<TObject> _queryObjects;
        private readonly Queue<int> _queryCriticalChildrenIndexes;
        private AABB2D _queryBounds, _extendedBounds;
        private float _currentExtendedEpsilon;

        private static bool IsSkippedObject => Random.Range(0f, 1f) > 0.5f;
        
        private int LastLevelTreeIndex => _levelTrees.Length - 1;

        private struct LinkLevelTreeData
        {
            public int bottomTreeIndex;
            public int topTreeIndex;
            
            public int bottomNodeCopyIndex;
            public int topNodeCopyIndex;
        }
        
        private struct NodeCopyPointer
        {
            public enum Properties
            {
                PrevLevelNodeCopyIndex,
                NextLevelNodeCopyIndex
            }
            
            public int nextLevelNodeCopyIndex;
            public int prevLevelNodeCopyIndex;
        }
        
        public SkipQuadtree(int levelsQuantity, AABB2D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _levelTrees = new CompressedQuadtree<TObject>[Mathf.Min(levelsQuantity, MaxPossibleLevelsQuantity)];
            _nodeCopyPointersByLevel = new Dictionary<int, NodeCopyPointer>[_levelTrees.Length];
            _levelTreesObjectIndexes = new Dictionary<TObject, int[]>(initialObjectsCapacity);
            _queryObjects = new List<TObject>(capacity: initialObjectsCapacity);

            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _levelTrees[i] = new CompressedQuadtree<TObject>(bounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
            }

            _queryCriticalChildrenIndexes = new Queue<int>(capacity: _levelTrees[0].NodesCapacity);
            
            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _nodeCopyPointersByLevel[i] = new Dictionary<int, NodeCopyPointer>(capacity: maxDepth);
            }

            InitNodeCopyPointers();
        }

        public void DebugDraw(int treeLevel, Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawCube(_extendedBounds.Center, _extendedBounds.Size);
            
            _levelTrees[treeLevel].DebugDraw(relativeTransform, isPlaymodeOnly);
        }

        public IReadOnlyList<TObject> GetObjectsByLevel(int treeLevel)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _levelTrees.Length);
            var objectsByLevel = new List<TObject>(capacity: 10);
            
            foreach (var indexesByLevel in _levelTreesObjectIndexes.Values)
            {
                if (indexesByLevel[treeLevel] == Null)
                {
                    continue;
                }
                
                objectsByLevel.Add(_levelTrees[treeLevel].GetNodeObjectBy(indexesByLevel[treeLevel]).target);
            }
            
            return objectsByLevel;
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int[] targetNodeIndexesByLevels = FindTargetNodeIndexesByLevels(objBounds);
            
            _levelTrees[0].AddToNodeWith(targetNodeIndexesByLevels[0], obj, objBounds, out objectIndex);
            _levelTreesObjectIndexes.Add(obj, ArrayUtility.CreateArray(capacity: _levelTrees.Length, defaultValue: Null));
            _levelTreesObjectIndexes[obj][0] = objectIndex;

            bool isAbortAddingObjectToNextLevelTrees = false;
            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (IsSkippedObject)
                {
                    isAbortAddingObjectToNextLevelTrees = true;
                    break;
                }

                _levelTrees[i].AddToNodeWith(targetNodeIndexesByLevels[i], obj, objBounds, out int objectIndexForCurrentLevel);
                _levelTreesObjectIndexes[obj][i] = objectIndexForCurrentLevel;
            }

            if (isAbortAddingObjectToNextLevelTrees == false)
            {
                UpdateLinksBetweenLevelTrees();
            }
            
            return true;
        }

        public bool TryRemove(int objectIndex)
        {
            Assert.IsTrue(_levelTrees[0].ContainsObjectWith(objectIndex));
            TObject obj = _levelTrees[0].GetNodeObjectBy(objectIndex).target;
            
            bool isObjectRemoved = true;
            int[] objectIndexesByLevel = _levelTreesObjectIndexes[obj];
            
            for (int i = 0; i < objectIndexesByLevel.Length; i++)
            {
                if (objectIndexesByLevel[i] == Null)
                {
                    break;
                }

                isObjectRemoved &= _levelTrees[i].TryRemove(objectIndexesByLevel[i]);
            }
            
            if (isObjectRemoved)
            {
                _levelTreesObjectIndexes.Remove(obj);
            }
            
            return isObjectRemoved;
        }

        public void CleanUp()
        {
            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _levelTrees[i].CleanUp();
            }
            
            UpdateLinksBetweenLevelTrees();
        }

        public int Update(int objectIndex, AABB2D updatedObjBounds)
        {
            Assert.IsTrue(_levelTrees[0].ContainsObjectWith(objectIndex));
            TObject obj = _levelTrees[0].GetNodeObjectBy(objectIndex).target;
            
            int actualObjectIndex = _levelTrees[0].Update(objectIndex, updatedObjBounds);
            _levelTreesObjectIndexes[obj][0] = actualObjectIndex;
            
            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (_levelTreesObjectIndexes[obj][i] == Null)
                {
                    break;
                }
                
                int updatedObjectIndexForCurrentLevel = _levelTrees[i].Update(_levelTreesObjectIndexes[obj][i], updatedObjBounds);
                _levelTreesObjectIndexes[obj][i] = updatedObjectIndexForCurrentLevel;
            }
            
            return actualObjectIndex;
        }

        public IReadOnlyList<TObject> ApproximateQuery(AABB2D queryBounds, float epsilon = DefaultExtendedEpsilon)
        {
            _queryObjects.Clear();
            
            _queryBounds = queryBounds;
            _currentExtendedEpsilon = epsilon;
            _extendedBounds = new AABB2D(queryBounds.Center, queryBounds.Extents + _currentExtendedEpsilon * Vector2.one);
            
            _queryCriticalChildrenIndexes.Enqueue(CompressedQuadtree<TObject>.RootIndex);

            int nodeIndex;
            Node node;

            while (_queryCriticalChildrenIndexes.Count != 0)
            {
                nodeIndex = _queryCriticalChildrenIndexes.Dequeue();
                node = _levelTrees[0].GetNodeBy(nodeIndex);
            
                if (queryBounds.Intersects(node.bounds) == false)
                {
                    continue;
                }
            
                if (node.isLeaf)
                {
                    _levelTrees[0].AddIntersectedQueryBoundsLeafObjects(nodeIndex, queryBounds, _queryObjects);
                    continue;
                }
                
                if (queryBounds.Contains(node.bounds))
                {
                    _levelTrees[0].DeepAddNodeObjects(nodeIndex, _queryObjects);
                    continue;
                }
                
                if (IsCriticalNodeOn(nodeIndex, treeLevel: 0) == false)
                {
                    int criticalNodeIndex = GetCriticalNodeIndexOnMinTreeLevel(nodeIndex);
                    _queryCriticalChildrenIndexes.Enqueue(criticalNodeIndex);
                    continue;
                }
                
                for (int i = 0; i < 4; i++)
                {
                    _queryCriticalChildrenIndexes.Enqueue(node.firstChildIndex + i);
                }
            }
            
            return _queryObjects;
        }

        private int GetCriticalNodeIndexOnMinTreeLevel(int nodeIndex)
        {
            Assert.IsTrue(_levelTrees[0].ContainsNodeWith(nodeIndex));
            int notCriticalNodeIndex = GetNotCriticalNodeIndexOnMaxTreeLevel(nodeIndex, out int maxTreeLevel);

            while (true)
            {
                if (IsCriticalNodeOn(notCriticalNodeIndex, maxTreeLevel) == false)
                {
                    int childIndexEqualTo = GetChildIndexEqualTo(maxTreeLevel, notCriticalNodeIndex);
                            
                    if (_levelTrees[maxTreeLevel].GetNodeBy(childIndexEqualTo).isLeaf == false || maxTreeLevel == 0)
                    {
                        notCriticalNodeIndex = childIndexEqualTo;
                    }
                    else if (maxTreeLevel != 0)
                    {
                        FindNodeCopyIndex(notCriticalNodeIndex, maxTreeLevel, maxTreeLevel - 1, out notCriticalNodeIndex);
                        maxTreeLevel--;
                    }
                }
                else if (maxTreeLevel != 0)
                {
                    FindNodeCopyIndex(notCriticalNodeIndex, maxTreeLevel, maxTreeLevel - 1, out notCriticalNodeIndex);
                    maxTreeLevel--;
                }
                else
                {
                    break;
                }
            }

            return notCriticalNodeIndex;
        }

        private int GetNotCriticalNodeIndexOnMaxTreeLevel(int nodeIndex, out int maxTreeLevel)
        {
            Assert.IsTrue(_levelTrees[0].ContainsNodeWith(nodeIndex));
            maxTreeLevel = LastLevelTreeIndex;

            while (maxTreeLevel > 0)
            {
                if (FindNodeCopyIndex(nodeIndex, 0, maxTreeLevel, out int foundNotCriticalNodeIndex) &&
                    IsCriticalNodeOn(foundNotCriticalNodeIndex, maxTreeLevel) == false)
                {
                    return foundNotCriticalNodeIndex;
                }

                maxTreeLevel--;
            }

            return nodeIndex;
        }

        private bool IsCriticalNodeOn(int nodeIndex, int treeLevel)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[treeLevel].ContainsNodeWith(nodeIndex));

            Node node = _levelTrees[treeLevel].GetNodeBy(nodeIndex);

            if (node.isLeaf)
            {
                return true;
            }

            int childIndex;
            AABB2D childBounds;
            bool isIntersectionAreaEqualToParent;
            float intersectionAreaForParentBounds = node.bounds.IntersectionArea(_extendedBounds);
            
            for (int i = 0; i < 4; i++)
            {
                childIndex = node.firstChildIndex + i;
                childBounds = _levelTrees[treeLevel].GetNodeBy(childIndex).bounds;
                
                isIntersectionAreaEqualToParent = MathUtility.IsApproximateEqual(childBounds.IntersectionArea(_extendedBounds), intersectionAreaForParentBounds);
                
                if (isIntersectionAreaEqualToParent && IsStabbingNodeWith(treeLevel, childIndex))
                {
                    return false;
                }
            }
            
            return true;
        }

        private int GetChildIndexEqualTo(int treeLevel, int branchIndex)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[treeLevel].ContainsNodeWith(branchIndex));

            Node branch = _levelTrees[treeLevel].GetNodeBy(branchIndex);
            Assert.IsTrue(branch.isLeaf == false);
            
            float intersectionAreaForParentBounds = branch.bounds.IntersectionArea(_extendedBounds);
            AABB2D childBounds;
            int childIndex;
            
            for (int i = 0; i < 4; i++)
            {
                childIndex = branch.firstChildIndex + i;
                childBounds = _levelTrees[treeLevel].GetNodeBy(childIndex).bounds;

                if (MathUtility.IsApproximateEqual(childBounds.IntersectionArea(_extendedBounds), intersectionAreaForParentBounds))
                {
                    return childIndex;
                }
            }
            
            return Null;
        }

        private bool IsStabbingNodeWith(int treeLevel, int nodeIndex)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[treeLevel].ContainsNodeWith(nodeIndex));
            
            AABB2D nodeBounds = _levelTrees[treeLevel].GetNodeBy(nodeIndex).bounds;
            
            if (MathUtility.IsLessOrEqual(nodeBounds.Extents.x, 2f * _currentExtendedEpsilon))
            {
                return false;
            }
            
            return _queryBounds.Intersects(nodeBounds) && _extendedBounds.Contains(nodeBounds) == false;
        }

        private int[] FindTargetNodeIndexesByLevels(AABB2D objBounds)
        {
            CompressedQuadtree<TObject> currentLevelTree;
            Dictionary<int, NodeCopyPointer> currentNodeCopyPointers;

            int[] targetNodeIndexesByLevels = ArrayUtility.CreateArray(capacity: _levelTrees.Length, defaultValue: CompressedQuadtree<TObject>.RootIndex);
            int nextLevelTreeNodeIndex = CompressedQuadtree<TObject>.RootIndex;
            
            for (int i = LastLevelTreeIndex; i >= 0; i--)
            {
                currentLevelTree = _levelTrees[i];
                currentNodeCopyPointers = _nodeCopyPointersByLevel[i];

                currentLevelTree.TraverseFrom(nextLevelTreeNodeIndex, data =>
                {
                    if (data.node.bounds.Intersects(objBounds))
                    {
                        targetNodeIndexesByLevels[i] = data.nodeIndex;

                        if (i == 0 || data.node.parentIndex == Null)
                        {
                            return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                        }
                        
                        int actualNodeIndex = data.node.isLeaf ? data.node.parentIndex : data.nodeIndex;
                        
                        if (currentNodeCopyPointers.TryGetValue(actualNodeIndex, out NodeCopyPointer foundNodeCopyPointer))
                        {
                            if (foundNodeCopyPointer.prevLevelNodeCopyIndex == Null)
                            {
                                if (FindNodeCopyIndex(actualNodeIndex, i, i - 1, out int bottomNodeCopyIndex) == false)
                                {
                                    return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                                }
                                
                                TryLinkTwoLevelTreesTogether(new LinkLevelTreeData
                                {
                                    bottomTreeIndex = i - 1,
                                    topTreeIndex = i,
                                    bottomNodeCopyIndex = bottomNodeCopyIndex,
                                    topNodeCopyIndex = actualNodeIndex
                                });

                                nextLevelTreeNodeIndex = bottomNodeCopyIndex;
                                return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                            }

                            nextLevelTreeNodeIndex = currentNodeCopyPointers[actualNodeIndex].prevLevelNodeCopyIndex;
                        }
                        
                        return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                    }

                    return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                });
            }

            return targetNodeIndexesByLevels;
        }

        private void UpdateLinksBetweenLevelTrees()
        {
            InitNodeCopyPointers();
            
            for (int i = LastLevelTreeIndex; i > 0; i--)
            {
                _levelTrees[i].TraverseFrom(CompressedQuadtree<TObject>.RootIndex, data =>
                {
                    if (data.node.isLeaf || data.node.parentIndex == Null || FindNodeCopyIndex(data.nodeIndex, i, i - 1, out int foundNodeCopyIndex) == false)
                    {
                        return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                    }

                    TryLinkTwoLevelTreesTogether(new LinkLevelTreeData
                    {
                        bottomTreeIndex = i - 1,
                        topTreeIndex = i,
                        bottomNodeCopyIndex = foundNodeCopyIndex,
                        topNodeCopyIndex = data.nodeIndex
                    });
                    
                    return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                });
            }
        }

        private bool FindNodeCopyIndex(int nodeIndex, int currentLevelTreeIndex, int requiredLevelTreeIndex, out int foundNodeCopyIndex)
        {
            Assert.IsTrue(currentLevelTreeIndex >= 0 && currentLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(requiredLevelTreeIndex >= 0 && requiredLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[currentLevelTreeIndex].ContainsNodeWith(nodeIndex));

            if (currentLevelTreeIndex == requiredLevelTreeIndex)
            {
                foundNodeCopyIndex = nodeIndex;
                return true;
            }
            
            bool isBackwardPass = Mathf.Sign(currentLevelTreeIndex - requiredLevelTreeIndex) > 0;
            int currentParentIndex = _levelTrees[currentLevelTreeIndex].GetNodeBy(nodeIndex).parentIndex;

            NodeCopyPointer nodeCopyPointer;
            
            for (int i = currentLevelTreeIndex; i != requiredLevelTreeIndex;)
            {
                if (_nodeCopyPointersByLevel[i].TryGetValue(currentParentIndex, out nodeCopyPointer) == false)
                {
                    nodeCopyPointer = GetInitialNodeCopyPointerByLevel(i);
                }

                currentParentIndex = isBackwardPass ? nodeCopyPointer.prevLevelNodeCopyIndex : nodeCopyPointer.nextLevelNodeCopyIndex;
                i += isBackwardPass ? -1 : 1;
            }

            AABB2D currentNodeCopyBounds = _levelTrees[currentLevelTreeIndex].GetNodeBy(nodeIndex).bounds;
            currentParentIndex = currentParentIndex == Null ? CompressedQuadtree<TObject>.RootIndex : currentParentIndex;
            
            foundNodeCopyIndex = _levelTrees[requiredLevelTreeIndex].GetEqualNodeIndexFrom(currentParentIndex, currentNodeCopyBounds);
            return foundNodeCopyIndex != Null;
        }

        private bool TryLinkTwoLevelTreesTogether(LinkLevelTreeData linkLevelTreeData)
        {
            Assert.IsTrue(linkLevelTreeData.bottomTreeIndex >= 0 && linkLevelTreeData.bottomTreeIndex < _levelTrees.Length);
            Assert.IsTrue(linkLevelTreeData.topTreeIndex >= 0 && linkLevelTreeData.topTreeIndex < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[linkLevelTreeData.bottomTreeIndex].ContainsNodeWith(linkLevelTreeData.bottomNodeCopyIndex));
            Assert.IsTrue(_levelTrees[linkLevelTreeData.topTreeIndex].ContainsNodeWith(linkLevelTreeData.topNodeCopyIndex));
                
            Node bottomLevelTreeNode = _levelTrees[linkLevelTreeData.bottomTreeIndex].GetNodeBy(linkLevelTreeData.bottomNodeCopyIndex);
            Node topLevelTreeNode = _levelTrees[linkLevelTreeData.topTreeIndex].GetNodeBy(linkLevelTreeData.topNodeCopyIndex);
                
            bool isBottomLevelTreeNodeBranch = bottomLevelTreeNode.isLeaf == false;
            bool isTopLevelTreeNodeBranch = topLevelTreeNode.isLeaf == false;
            bool isEqualNodes = bottomLevelTreeNode.bounds == topLevelTreeNode.bounds;

            if (isBottomLevelTreeNodeBranch && isTopLevelTreeNodeBranch && isEqualNodes)
            {
                SetNodeCopyPropertyValueTo(
                    linkLevelTreeData.bottomTreeIndex,
                    linkLevelTreeData.bottomNodeCopyIndex,
                    NodeCopyPointer.Properties.NextLevelNodeCopyIndex,
                    linkLevelTreeData.topNodeCopyIndex
                );
            
                SetNodeCopyPropertyValueTo(
                    linkLevelTreeData.topTreeIndex,
                    linkLevelTreeData.topNodeCopyIndex,
                    NodeCopyPointer.Properties.PrevLevelNodeCopyIndex,
                    linkLevelTreeData.bottomNodeCopyIndex
                );
                
                return true;
            }

            return false;
        }

        private void SetNodeCopyPropertyValueTo(int levelTreeIndex, int nodeCopyIndex, NodeCopyPointer.Properties nodeCopyProperty, int value)
        {
            Assert.IsTrue(levelTreeIndex >= 0 && levelTreeIndex < _levelTrees.Length);
            Dictionary<int, NodeCopyPointer> levelNodeCopyPointers = _nodeCopyPointersByLevel[levelTreeIndex];
            
            if (levelNodeCopyPointers.ContainsKey(nodeCopyIndex))
            {
                NodeCopyPointer nodeCopyPointer = levelNodeCopyPointers[nodeCopyIndex];

                if (nodeCopyProperty == NodeCopyPointer.Properties.PrevLevelNodeCopyIndex)
                {
                    nodeCopyPointer.prevLevelNodeCopyIndex = value;
                }
                
                if (nodeCopyProperty == NodeCopyPointer.Properties.NextLevelNodeCopyIndex)
                {
                    nodeCopyPointer.nextLevelNodeCopyIndex = value;
                }
                
                levelNodeCopyPointers[nodeCopyIndex] = nodeCopyPointer;
            }
            else
            {
                levelNodeCopyPointers.Add(nodeCopyIndex, new NodeCopyPointer
                {
                    prevLevelNodeCopyIndex = nodeCopyProperty == NodeCopyPointer.Properties.PrevLevelNodeCopyIndex ? value : Null,
                    nextLevelNodeCopyIndex = nodeCopyProperty == NodeCopyPointer.Properties.NextLevelNodeCopyIndex ? value : Null
                });
            }
        }

        private void InitNodeCopyPointers()
        {
            for (int i = 0; i < _nodeCopyPointersByLevel.Length; i++)
            {
                _nodeCopyPointersByLevel[i].Clear();
                _nodeCopyPointersByLevel[i].Add(CompressedQuadtree<TObject>.RootIndex, GetInitialNodeCopyPointerByLevel(i));
            }
        }

        private NodeCopyPointer GetInitialNodeCopyPointerByLevel(int treeLevel)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _nodeCopyPointersByLevel.Length);
            
            return new NodeCopyPointer
            {
                prevLevelNodeCopyIndex = treeLevel > 0 ? CompressedQuadtree<TObject>.RootIndex : Null,
                nextLevelNodeCopyIndex = treeLevel < _levelTrees.Length - 1 ? CompressedQuadtree<TObject>.RootIndex : Null
            };
        }
    }
}