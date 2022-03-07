using System.Collections.Generic;
using Codice.Client.ChangeTrackerService;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class SkipQuadtree<TObject> where TObject : class
    {
        private const sbyte NULL = -1;
        private const sbyte MAX_POSSIBLE_LEVELS_QUANTITY = 8;
        
        private readonly CompressedQuadtree<TObject>[] _levelTrees;
        private readonly Dictionary<int, NodeCopyPointer>[] _nodeCopyPointersByLevel;
        private readonly Dictionary<int, int[]> _levelTreesObjectIndexes;

        private int LastLevelTreeIndex => _levelTrees.Length - 1;
        private static bool IsSkippedObject => Random.Range(0f, 1f) > 0.5f;
        
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
        
        public SkipQuadtree(sbyte levelsQuantity, AABB2D bounds, sbyte maxLeafObjects, sbyte maxDepth, int initialObjectsCapacity)
        {
            _levelTrees = new CompressedQuadtree<TObject>[Mathf.Min(levelsQuantity, MAX_POSSIBLE_LEVELS_QUANTITY)];
            _nodeCopyPointersByLevel = new Dictionary<int, NodeCopyPointer>[_levelTrees.Length];
            _levelTreesObjectIndexes = new Dictionary<int, int[]>(initialObjectsCapacity);
            
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
            
            int[] targetNodeIndexes = FindTargetNodeIndexes(objBounds);
            bool isObjAdded = _levelTrees[0].TryAdd(targetNodeIndexes[0], obj, objBounds, out objectIndex);
            
            _levelTreesObjectIndexes.Add(objectIndex, CreateEmptyIndexesArrayByLevel(_levelTrees.Length));
            _levelTreesObjectIndexes[objectIndex][0] = objectIndex;

            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (IsSkippedObject || _levelTrees[i].TryAdd(targetNodeIndexes[i], obj, objBounds, out int objectIndexForCurrentLevel) == false)
                {
                    break;
                }

                _levelTreesObjectIndexes[objectIndex][i] = objectIndexForCurrentLevel;

                Node prevLevelTreeNode = _levelTrees[i - 1].GetNodeBy(targetNodeIndexes[i - 1]);
                Node currentLevelTreeNode = _levelTrees[i].GetNodeBy(targetNodeIndexes[i]);
                
                bool isPrevLevelTreeNodeBranch = prevLevelTreeNode.isLeaf == false;
                bool isCurrentLevelTreeNodeBranch = currentLevelTreeNode.isLeaf == false;
                bool isEqualNodes = prevLevelTreeNode.bounds == currentLevelTreeNode.bounds;
                
                if (isPrevLevelTreeNodeBranch && isCurrentLevelTreeNodeBranch && isEqualNodes)
                {
                    LinkTwoLevelTreesTogether(
                        i - 1,
                        i,
                        targetNodeIndexes[i - 1],
                        targetNodeIndexes[i]
                    );
                }
            }
            
            return isObjAdded;
        }

        public bool TryRemove(int objectIndex)
        {
            Assert.IsTrue(_levelTreesObjectIndexes.ContainsKey(objectIndex));

            bool isObjectRemoved = true;
            int[] objectIndexesByLevel = _levelTreesObjectIndexes[objectIndex];
            
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
                _levelTreesObjectIndexes.Remove(objectIndex);
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

        public int Update(int objectIndex, TObject updatedObj, AABB2D updatedObjBounds)
        {
            Assert.IsTrue(_levelTreesObjectIndexes.ContainsKey(objectIndex));
            Assert.IsNotNull(updatedObj);

            int actualObjectIndex = _levelTrees[0].Update(objectIndex, updatedObj, updatedObjBounds);
            if (actualObjectIndex != objectIndex)
            {
                int[] tempObjectIndexesForLevels = _levelTreesObjectIndexes[objectIndex];
                _levelTreesObjectIndexes.Remove(objectIndex);
                _levelTreesObjectIndexes.Add(actualObjectIndex, tempObjectIndexesForLevels);
            }
            
            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (_levelTreesObjectIndexes[actualObjectIndex][i] == NULL)
                {
                    break;
                }
                
                int updatedObjectIndexForCurrentLevel = _levelTrees[i].Update(_levelTreesObjectIndexes[actualObjectIndex][i], updatedObj, updatedObjBounds);
                _levelTreesObjectIndexes[actualObjectIndex][i] = updatedObjectIndexForCurrentLevel;
            }

            return actualObjectIndex;
        }

        private int[] FindTargetNodeIndexes(AABB2D objBounds)
        {
            CompressedQuadtree<TObject> currentLevelTree;
            Dictionary<int, NodeCopyPointer> currentNodeCopyPointers;

            int[] targetNodeIndexes = CreateEmptyIndexesArrayByLevel(_levelTrees.Length);
            int nextLevelTreeNodeIndex = _levelTrees[LastLevelTreeIndex].RootIndex;

            for (int i = LastLevelTreeIndex; i >= 0; i--)
            {
                currentLevelTree = _levelTrees[i];
                currentNodeCopyPointers = _nodeCopyPointersByLevel[i];

                currentLevelTree.TraverseFrom(nextLevelTreeNodeIndex, (nodeIndex, parentIndex) =>
                {
                    Node node = currentLevelTree.GetNodeBy(nodeIndex);
                
                    if (node.bounds.Intersects(objBounds))
                    {
                        targetNodeIndexes[i] = nodeIndex;
                        int actualNodeIndex = node.isLeaf
                            ? parentIndex == NULL ? currentLevelTree.RootIndex : parentIndex
                            : nodeIndex;

                        if (currentNodeCopyPointers.TryGetValue(actualNodeIndex, out NodeCopyPointer foundNodeCopyPointer))
                        {
                            nextLevelTreeNodeIndex = foundNodeCopyPointer.prevLevelCopyIndex;
                        }

                        return CompressedQuadtree<TObject>.ExecutionSignal.ContinueInDepth;
                    }

                    return CompressedQuadtree<TObject>.ExecutionSignal.Continue;
                });
            }

            return targetNodeIndexes;
        }

        private void LinkTwoLevelTreesTogether(int bottomLevelTreeIndex, int topLevelTreeIndex, int bottomLevelNodeCopyIndex, int topLevelNodeCopyIndex)
        {
            Assert.IsTrue(bottomLevelTreeIndex >= 0 && bottomLevelTreeIndex < _levelTrees.Length);
            Assert.IsTrue(topLevelTreeIndex >= 0 && topLevelTreeIndex < _levelTrees.Length);

            SetNodeCopyPropertyValueTo(
                bottomLevelTreeIndex,
                bottomLevelNodeCopyIndex,
                NodeCopyPointer.Properties.NextLevelCopyIndex,
                topLevelNodeCopyIndex
            );
            
            SetNodeCopyPropertyValueTo(
                topLevelTreeIndex,
                topLevelNodeCopyIndex,
                NodeCopyPointer.Properties.PrevLevelCopyIndex,
                bottomLevelNodeCopyIndex
            );
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

        private int[] CreateEmptyIndexesArrayByLevel(int capacity)
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