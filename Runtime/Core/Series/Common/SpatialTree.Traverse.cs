using System;
using UnityEngine.Assertions;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        internal void TraverseFrom(int nodeIndex, Func<TraverseData<TBounds, TVector>, ExecutionSignal> eachNodeAction, bool needTraverseForStartNode = true)
        {
            Assert.IsTrue(_nodes.Contains(nodeIndex));
            Assert.IsNotNull(eachNodeAction);
            
            if (_nodes[nodeIndex].isLeaf)
            {
                if (needTraverseForStartNode)
                {
                    eachNodeAction.Invoke(new TraverseData<TBounds, TVector>(nodeIndex, _nodes[nodeIndex], false, false));   
                }
                return;
            }
            
            _branchIndexes[0] = nodeIndex;

            ExecutionSignal executionSignal;
            if (needTraverseForStartNode)
            {
                executionSignal = eachNodeAction.Invoke(new TraverseData<TBounds, TVector>(nodeIndex, _nodes[nodeIndex], false, false));
                if (executionSignal == ExecutionSignal.Stop)
                {
                    return;
                }
            }

            int parentIndex, firstChildIndex, childIndex;
            Node<TBounds, TVector> childNode;
            bool isParentChanged;

            for (int traverseBranchIndex = 0, freeBranchIndex = 1;
                traverseBranchIndex < _branchIndexes.Length && _branchIndexes[traverseBranchIndex] != Null;
                traverseBranchIndex++)
            {
                parentIndex = _branchIndexes[traverseBranchIndex];
                isParentChanged = true;
                firstChildIndex = _nodes[parentIndex].firstChildIndex;

                for (int i = 0; i < _maxChildrenCount; i++)
                {
                    childIndex = firstChildIndex + i;
                    childNode = _nodes[childIndex];
                    
                    executionSignal = eachNodeAction.Invoke(new TraverseData<TBounds, TVector>(
                        childIndex,
                        childNode,
                        isParentChanged,
                        i == (_maxChildrenCount - 1)
                    ));
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
        
        internal void TraverseFromRoot(Func<TraverseData<TBounds, TVector>, ExecutionSignal> eachNodeAction, bool needTraverseForStartNode = true)
        {
            Assert.IsNotNull(eachNodeAction);
            TraverseFrom(RootIndex, eachNodeAction, needTraverseForStartNode);
        }

        internal int GetEqualNodeIndexFrom(int nodeIndex, TBounds equalBounds)
        {
            Assert.IsTrue(_nodes.Contains(nodeIndex));
            
            _tempEqualNodeIndex = Null;
            _tempBounds = equalBounds;
            
            TraverseFrom(nodeIndex, data =>
            {
                if (data.node.bounds.Equals(_tempBounds))
                {
                    _tempEqualNodeIndex = data.nodeIndex;
                    return ExecutionSignal.Stop;
                }

                return ExecutionSignal.Continue;
            });
            
            return _tempEqualNodeIndex;
        }
        
        private void ClearBranchIndexes(int fromIndex, int toIndex)
        {
            Assert.IsTrue(fromIndex >= 0 && fromIndex < _branchIndexes.Length);
            Assert.IsTrue(toIndex >= 0 && toIndex < _branchIndexes.Length);
            Assert.IsTrue(fromIndex < toIndex);
            
            for (int i = fromIndex; i < toIndex && _branchIndexes[i] != Null; i++)
            {
                _branchIndexes[i] = Null;
            }
        }
    }
}
