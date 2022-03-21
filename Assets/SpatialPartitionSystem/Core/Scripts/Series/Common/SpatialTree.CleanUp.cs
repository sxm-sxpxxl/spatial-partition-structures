using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal partial class SpatialTree<TObject> : ISpatialTree<TObject> where TObject : class
    {
        internal int CurrentBranchCount => _nodes.Count / 4;
        
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
            Assert.IsFalse(_nodes[branchIndex].isLeaf);

            Node node = _nodes[branchIndex];
            
            for (int i = 0; i < 4; i++)
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
                Node targetNode;
                
                TraverseFromRoot(data =>
                {
                    targetNode = _nodes[data.nodeIndex];
                    
                    if (currentParentIndex != targetNode.parentIndex)
                    {
                        currentParentIndex = targetNode.parentIndex;
                        childrenObjectsCount = 0;
                        hasBranchAmongChildren = false;
                    }
                    
                    if (hasBranchAmongChildren == false && targetNode.isLeaf)
                    {
                        childrenObjectsCount += targetNode.objectsCount;
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
            Assert.IsFalse(_nodes[parentBranchIndex].isLeaf);
            
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
    }
}
