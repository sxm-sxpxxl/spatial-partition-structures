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
        private readonly List<TObject>[] _levelTreesObjects;

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
            _levelTreesObjects = new List<TObject>[_levelTrees.Length];
            
            for (int i = 0; i < _levelTrees.Length; i++)
            {
                _levelTrees[i] = new CompressedQuadtree<TObject>(bounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
                _levelTreesObjects[i] = new List<TObject>(capacity: 10);
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

        public List<TObject> GetObjectsByLevel(int treeLevel)
        {
            return _levelTreesObjects[treeLevel];
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int[] targetNodeIndexes = FindTargetNodeIndexes(objBounds);
            bool isObjAdded = _levelTrees[0].TryAdd(targetNodeIndexes[0], obj, objBounds, out objectIndex);
            
            _levelTreesObjects[0].Add(obj);

            for (int i = 1; i < _levelTrees.Length; i++)
            {
                if (IsSkippedObject || _levelTrees[i].TryAdd(targetNodeIndexes[i], obj, objBounds, out _) == false)
                {
                    break;
                }

                _levelTreesObjects[i].Add(obj);

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
            return true;
        }

        public void CleanUp()
        {

        }

        private int[] FindTargetNodeIndexes(AABB2D objBounds)
        {
            CompressedQuadtree<TObject> currentLevelTree;
            Dictionary<int, NodeCopyPointer> currentNodeCopyPointers;

            int[] targetNodeIndexes = new int[_levelTrees.Length];
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
    }
}