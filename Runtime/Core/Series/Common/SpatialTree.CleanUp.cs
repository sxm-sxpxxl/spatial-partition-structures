﻿using UnityEngine.Assertions;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        internal int CurrentBranchCount => _nodes.Count / _maxChildrenCount;
        
        public void CleanUp()
        {
            if (CurrentBranchCount == 0)
            {
                return;
            }

            DeepCleanUp();
        }

        internal void DeleteChildrenNodesFor(int branchIndex)
        {
            Assert.IsTrue(branchIndex >= 0 && branchIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[branchIndex].isLeaf == false);

            var node = _nodes[branchIndex];
            
            for (int i = 0; i < _maxChildrenCount; i++)
            {
                _nodes.RemoveAt(node.firstChildIndex + i);
            }

            node.firstChildIndex = Null;
            node.isLeaf = true;

            _nodes[branchIndex] = node;
        }
        
        internal void DeepCleanUp()
        {
            bool isNeedAnotherCleanUp;
            do
            {
                isNeedAnotherCleanUp = false;
                
                int currentParentIndex = Null, childrenObjectsCount = 0;
                bool hasBranchAmongChildren = false;
                
                TraverseFromRoot(data =>
                {
                    if (currentParentIndex != data.node.parentIndex)
                    {
                        currentParentIndex = data.node.parentIndex;
                        childrenObjectsCount = 0;
                        hasBranchAmongChildren = false;
                    }
                    
                    if (hasBranchAmongChildren == false && data.node.isLeaf)
                    {
                        childrenObjectsCount += data.node.objectsCount;
                    }
                    else
                    {
                        hasBranchAmongChildren = true;
                    }

                    if (data.isLastChild && hasBranchAmongChildren == false && childrenObjectsCount <= _maxLeafObjects)
                    {
                        LocalCleanUp(currentParentIndex, childrenObjectsCount);
                        isNeedAnotherCleanUp = true;
                    }
                
                    return ExecutionSignal.Continue;
                }, needTraverseForStartNode: false);
            } while (isNeedAnotherCleanUp);
        }
        
        private void LocalCleanUp(int parentBranchIndex, int childrenObjectsCount)
        {
            Assert.IsTrue(parentBranchIndex >= 0 && parentBranchIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[parentBranchIndex].isLeaf == false);
            
            int firstChildIndex = _nodes[parentBranchIndex].firstChildIndex;
            int[] childrenUnlinkedPointerIndexes = new int[childrenObjectsCount];
            
            for (int i = 0, j = 0; i < _maxChildrenCount; i++)
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
                LinkObjectTo(parentBranchIndex, childrenUnlinkedPointerIndexes[i]);
            }
        }
    }
}
