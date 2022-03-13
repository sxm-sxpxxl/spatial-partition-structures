using System.Collections.Generic;
using Codice.Client.ChangeTrackerService;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class SkipQuadtree<TObject> where TObject : class
    {
        private const int NULL = -1, MAX_POSSIBLE_LEVELS_QUANTITY = 8;
        
        private readonly CompressedQuadtree<TObject>[] _levelTrees;
        private readonly Dictionary<int, NodeCopyPointer>[] _nodeCopyPointersByLevel;
        private readonly Dictionary<TObject, int[]> _levelTreesObjectIndexes;

        private static bool IsSkippedObject => Random.Range(0f, 1f) > 0.5f;
        
        private int LastLevelTreeIndex => _levelTrees.Length - 1;

        private struct LinkData
        {
            public int bottomLevelTreeIndex;
            public int topLevelTreeIndex;
            
            public int bottomLevelTreeNodeCopyIndex;
            public int topLevelTreeNodeCopyIndex;
        }
        
        private struct NodeCopyPointer
        {
            public enum Properties
            {
                PrevLevelCopyIndex,
                NextLevelCopyIndex
            }
            
            public int nextLevelCopyIndex;
            public int prevLevelCopyIndex;
        }
        
        public SkipQuadtree(int levelsQuantity, AABB2D bounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _levelTrees = new CompressedQuadtree<TObject>[Mathf.Min(levelsQuantity, MAX_POSSIBLE_LEVELS_QUANTITY)];
            _nodeCopyPointersByLevel = new Dictionary<int, NodeCopyPointer>[_levelTrees.Length];
            _levelTreesObjectIndexes = new Dictionary<TObject, int[]>(initialObjectsCapacity);
            
            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _levelTrees[i] = new CompressedQuadtree<TObject>(bounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
            }
            
            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _nodeCopyPointersByLevel[i] = new Dictionary<int, NodeCopyPointer>(capacity: maxDepth);

                NodeCopyPointer nodeCopyPointer = new NodeCopyPointer
                {
                    prevLevelCopyIndex = i > 0 ? _levelTrees[i - 1].RootIndex : NULL,
                    nextLevelCopyIndex = i < _levelTrees.Length - 1 ? _levelTrees[i + 1].RootIndex : NULL
                };
                
                _nodeCopyPointersByLevel[i].Add(_levelTrees[i].RootIndex, nodeCopyPointer);
            }
        }

        public void DebugDraw(int treeLevel, Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            _levelTrees[treeLevel].DebugDraw(relativeTransform, isPlaymodeOnly);
        }

        public IReadOnlyList<TObject> GetObjectsByLevel(int treeLevel)
        {
            Assert.IsTrue(treeLevel >= 0 && treeLevel < _levelTrees.Length);

            var objectsByLevel = new List<TObject>(capacity: 10);
            
            foreach (var indexesByLevel in _levelTreesObjectIndexes.Values)
            {
                if (indexesByLevel[treeLevel] == NULL)
                {
                    continue;
                }
                
                objectsByLevel.Add(_levelTrees[treeLevel].GetObjectBy(indexesByLevel[treeLevel]));
            }
            
            return objectsByLevel;
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int[] targetNodeIndexesByLevels = FindTargetNodeIndexesByLevels(objBounds);
            
            _levelTrees[0].AddToNodeWith(targetNodeIndexesByLevels[0], obj, objBounds, out objectIndex);
            _levelTreesObjectIndexes.Add(obj, CreateEmptyIndexesArrayByLevel(_levelTrees.Length));
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
            TObject obj = _levelTrees[0].GetObjectBy(objectIndex);
            
            bool isObjectRemoved = true;
            int[] objectIndexesByLevel = _levelTreesObjectIndexes[obj];
            
            for (int i = 0; i < objectIndexesByLevel.Length; i++)
            {
                if (objectIndexesByLevel[i] == NULL)
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
        }

        public int Update(int objectIndex, AABB2D updatedObjBounds)
        {
            Assert.IsTrue(_levelTrees[0].ContainsObjectWith(objectIndex));
            TObject obj = _levelTrees[0].GetObjectBy(objectIndex);
            
            int actualObjectIndex = _levelTrees[0].Update(objectIndex, updatedObjBounds);
            _levelTreesObjectIndexes[obj][0] = actualObjectIndex;
            
            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (_levelTreesObjectIndexes[obj][i] == NULL)
                {
                    break;
                }
                
                int updatedObjectIndexForCurrentLevel = _levelTrees[i].Update(_levelTreesObjectIndexes[obj][i], updatedObjBounds);
                _levelTreesObjectIndexes[obj][i] = updatedObjectIndexForCurrentLevel;
            }

            return actualObjectIndex;
        }

        private int[] FindTargetNodeIndexesByLevels(AABB2D objBounds)
        {
            CompressedQuadtree<TObject> currentLevelTree;
            Dictionary<int, NodeCopyPointer> currentNodeCopyPointers;

            int[] targetNodeIndexesByLevels = CreateEmptyIndexesArrayByLevel(_levelTrees.Length);
            int nextLevelTreeNodeIndex = _levelTrees[LastLevelTreeIndex].RootIndex;
            
            for (int i = LastLevelTreeIndex; i >= 0; i--)
            {
                currentLevelTree = _levelTrees[i];
                currentNodeCopyPointers = _nodeCopyPointersByLevel[i];

                currentLevelTree.TraverseFrom(nextLevelTreeNodeIndex, data =>
                {
                    Node node = currentLevelTree.GetNodeBy(data.nodeIndex);
                
                    if (node.bounds.Intersects(objBounds))
                    {
                        targetNodeIndexesByLevels[i] = data.nodeIndex;

                        if (i == 0 || data.parentIndex == NULL)
                        {
                            return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                        }
                        
                        int actualNodeIndex = node.isLeaf ? data.parentIndex : data.nodeIndex;
                        
                        if (currentNodeCopyPointers.TryGetValue(actualNodeIndex, out NodeCopyPointer foundNodeCopyPointer))
                        {
                            Assert.IsFalse(foundNodeCopyPointer.prevLevelCopyIndex == NULL);
                            nextLevelTreeNodeIndex = foundNodeCopyPointer.prevLevelCopyIndex;
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
            CompressedQuadtree<TObject> currentLevelTree;
            Dictionary<int, NodeCopyPointer> currentNodeCopyPointers;

            int nextLevelTreeNodeIndex = _levelTrees[LastLevelTreeIndex].RootIndex;
            
            for (int i = LastLevelTreeIndex; i > 0; i--)
            {
                currentLevelTree = _levelTrees[i];
                currentNodeCopyPointers = _nodeCopyPointersByLevel[i];

                currentLevelTree.TraverseFrom(nextLevelTreeNodeIndex, data =>
                {
                    Node node = currentLevelTree.GetNodeBy(data.nodeIndex);

                    if (node.isLeaf || data.parentIndex == NULL || FindNodeCopyIndex(data.nodeIndex, data.parentIndex, i, i - 1, out int foundNodeCopyIndex) == false)
                    {
                        return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                    }

                    int actualNodeIndex = TryLinkTwoLevelTreesTogether(new LinkData
                    {
                        bottomLevelTreeIndex = i - 1,
                        topLevelTreeIndex = i,
                        bottomLevelTreeNodeCopyIndex = foundNodeCopyIndex,
                        topLevelTreeNodeCopyIndex = data.nodeIndex
                    }) ? data.nodeIndex : data.parentIndex;

                    Assert.IsFalse(currentNodeCopyPointers[actualNodeIndex].prevLevelCopyIndex == NULL);
                    nextLevelTreeNodeIndex = currentNodeCopyPointers[actualNodeIndex].prevLevelCopyIndex;
                    
                    return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                });
            }
        }

        private bool FindNodeCopyIndex(int nodeIndex, int parentIndex, int currentLevelTreeIndex, int requiredLevelTreeIndex, out int foundNodeCopyIndex)
        {
            Assert.IsTrue(currentLevelTreeIndex >= 0 && currentLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(requiredLevelTreeIndex >= 0 && requiredLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[currentLevelTreeIndex].ContainsNodeWith(nodeIndex));
            Assert.IsTrue(_levelTrees[currentLevelTreeIndex].ContainsNodeWith(parentIndex));

            if (nodeIndex == parentIndex)
            {
                foundNodeCopyIndex = nodeIndex;
                return true;
            }
            
            bool isBackwardPass = Mathf.Sign(currentLevelTreeIndex - requiredLevelTreeIndex) > 0;
            int currentParentIndex = parentIndex;

            NodeCopyPointer nodeCopyPointer;
            
            for (int i = currentLevelTreeIndex; i != requiredLevelTreeIndex;)
            {
                if (_nodeCopyPointersByLevel[i].TryGetValue(currentParentIndex, out nodeCopyPointer) == false)
                {
                    foundNodeCopyIndex = NULL;
                    return false;
                }
                
                currentParentIndex = isBackwardPass ? nodeCopyPointer.prevLevelCopyIndex : nodeCopyPointer.nextLevelCopyIndex;
                i += isBackwardPass ? -1 : 1;
            }
            
            AABB2D currentNodeCopyBounds = _levelTrees[currentLevelTreeIndex].GetNodeBy(parentIndex).bounds;
            int requiredChildCopyIndex = NULL;
            
            _levelTrees[requiredLevelTreeIndex].TraverseFrom(currentParentIndex, data =>
            {
                Node node = _levelTrees[requiredLevelTreeIndex].GetNodeBy(data.nodeIndex);

                if (node.isLeaf)
                {
                    return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                }

                if (node.bounds == currentNodeCopyBounds)
                {
                    requiredChildCopyIndex = data.nodeIndex;
                    return CompressedQuadtree<TObject>.ExecutionSignal.Stop;
                }

                return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
            });

            foundNodeCopyIndex = requiredChildCopyIndex;
            return foundNodeCopyIndex != NULL;
        }

        private bool TryLinkTwoLevelTreesTogether(LinkData linkData)
        {
            Assert.IsTrue(linkData.bottomLevelTreeIndex >= 0 && linkData.bottomLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(linkData.topLevelTreeIndex >= 0 && linkData.topLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(_levelTrees[linkData.bottomLevelTreeIndex].ContainsNodeWith(linkData.bottomLevelTreeNodeCopyIndex));
            Assert.IsTrue(_levelTrees[linkData.topLevelTreeIndex].ContainsNodeWith(linkData.topLevelTreeNodeCopyIndex));
                
            Node bottomLevelTreeNode = _levelTrees[linkData.bottomLevelTreeIndex].GetNodeBy(linkData.bottomLevelTreeNodeCopyIndex);
            Node topLevelTreeNode = _levelTrees[linkData.topLevelTreeIndex].GetNodeBy(linkData.topLevelTreeNodeCopyIndex);
                
            bool isBottomLevelTreeNodeBranch = bottomLevelTreeNode.isLeaf == false;
            bool isTopLevelTreeNodeBranch = topLevelTreeNode.isLeaf == false;
            bool isEqualNodes = bottomLevelTreeNode.bounds == topLevelTreeNode.bounds;

            if (isBottomLevelTreeNodeBranch && isTopLevelTreeNodeBranch && isEqualNodes)
            {
                SetNodeCopyPropertyValueTo(
                    linkData.bottomLevelTreeIndex,
                    linkData.bottomLevelTreeNodeCopyIndex,
                    NodeCopyPointer.Properties.NextLevelCopyIndex,
                    linkData.topLevelTreeNodeCopyIndex
                );
            
                SetNodeCopyPropertyValueTo(
                    linkData.topLevelTreeIndex,
                    linkData.topLevelTreeNodeCopyIndex,
                    NodeCopyPointer.Properties.PrevLevelCopyIndex,
                    linkData.bottomLevelTreeNodeCopyIndex
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

                if (nodeCopyProperty == NodeCopyPointer.Properties.PrevLevelCopyIndex)
                {
                    nodeCopyPointer.prevLevelCopyIndex = value;
                }
                
                if (nodeCopyProperty == NodeCopyPointer.Properties.NextLevelCopyIndex)
                {
                    nodeCopyPointer.nextLevelCopyIndex = value;
                }
                
                levelNodeCopyPointers[nodeCopyIndex] = nodeCopyPointer;
            }
            else
            {
                levelNodeCopyPointers.Add(nodeCopyIndex, new NodeCopyPointer
                {
                    prevLevelCopyIndex = nodeCopyProperty == NodeCopyPointer.Properties.PrevLevelCopyIndex ? value : NULL,
                    nextLevelCopyIndex = nodeCopyProperty == NodeCopyPointer.Properties.NextLevelCopyIndex ? value : NULL
                });
            }
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
    }
}