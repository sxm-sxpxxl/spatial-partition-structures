using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series.Trees
{
    public class BaseCompressedTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>, IQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private const int Null = SpatialTree<TObject, TBounds, TVector>.Null;
        
        private readonly SpatialTree<TObject, TBounds, TVector> _tree;

        internal int NodesCapacity => _tree.NodesCapacity;
        
        private struct CompressData
        {
            public int nodeIndex;
            public int firstInterestingNodeIndex;
            public int[] notInterestingNodeIndexes;

            public bool HasNotInterestingNodeWith(int targetNodeIndex)
            {
                if (notInterestingNodeIndexes == null)
                {
                    return false;
                }
                
                for (int i = 0; i < notInterestingNodeIndexes.Length; i++)
                {
                    if (notInterestingNodeIndexes[i] == targetNodeIndex)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        
        public BaseCompressedTree(Dimension dimension, TBounds rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _tree = new SpatialTree<TObject, TBounds, TVector>(dimension, rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
        }

        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            _tree.DebugDraw(relativeTransform, isPlaymodeOnly);
        }

        public bool TryAdd(TObject obj, TBounds objBounds, out int objectIndex)
        {
            Assert.IsNotNull(obj);
            
            int targetNodeIndex = _tree.FindTargetNodeIndex(objBounds);
            if (targetNodeIndex == Null)
            {
                objectIndex = Null;
                return false;
            }
            
            AddToNodeWith(targetNodeIndex, obj, objBounds, out objectIndex);
            return true;
        }

        public bool TryRemove(int objectIndex)
        {
            return _tree.TryRemove(objectIndex);
        }

        public int Update(int objectIndex, TBounds updatedObjBounds)
        {
            return _tree.Update(objectIndex, updatedObjBounds);
        }

        public void CleanUp()
        {
            if (_tree.CurrentBranchCount == 0)
            {
                return;
            }

            DeepCompress();
            _tree.DeepCleanUp();
        }

        public IReadOnlyList<TObject> Query(TBounds queryBounds)
        {
            return _tree.Query(queryBounds);
        }
        
        internal void AddToNodeWith(int nodeIndex, TObject obj, TBounds objBounds, out int objectIndex)
        {
            Assert.IsTrue(_tree.ContainsNodeWith(nodeIndex));
            Assert.IsNotNull(obj);
            
            if (_tree.GetNodeBy(nodeIndex).isLeaf)
            {
                objectIndex = _tree.AddObjectToLeaf(nodeIndex, obj, objBounds);
                Compress(nodeIndex);
            }
            else
            {
                int decompressedTargetLeafIndex = Decompress(nodeIndex, objBounds);
                objectIndex = _tree.AddObjectToLeaf(decompressedTargetLeafIndex, obj, objBounds);
                Compress(nodeIndex);
            }
        }

        internal Node<TBounds, TVector> GetNodeBy(int nodeIndex) => _tree.GetNodeBy(nodeIndex);

        internal NodeObject<TObject, TBounds, TVector> GetNodeObjectBy(int objectIndex) => _tree.GetNodeObjectBy(objectIndex);

        internal bool ContainsNodeWith(int nodeIndex) => _tree.ContainsNodeWith(nodeIndex);

        internal bool ContainsObjectWith(int objectIndex) => _tree.ContainsObjectWith(objectIndex);

        internal void AddIntersectedLeafObjects(int leafIndex, TBounds queryBounds, ICollection<TObject> objects)
        {
            _tree.AddLeafObjects(leafIndex, objects, new SpatialTree<TObject, TBounds, TVector>.AddObjectRequest
            {
                queryBounds = queryBounds,
                needIntersectionCheck = true
            });
        }

        internal void DeepAddNodeObjects(int nodeIndex, ICollection<TObject> objects) => _tree.DeepAddNodeObjects(nodeIndex, objects);

        internal int GetEqualNodeIndexFrom(int nodeIndex, TBounds equalBounds) => _tree.GetEqualNodeIndexFrom(nodeIndex, equalBounds);

        internal void TraverseFrom(int nodeIndex, Func<TraverseData<TBounds, TVector>, ExecutionSignal> eachNodeAction, bool needTraverseForStartNode = true)
        {
            _tree.TraverseFrom(nodeIndex, eachNodeAction, needTraverseForStartNode);
        }
        
        private void DeepCompress()
        {
            CompressData[] rootInterestingBranchesCompressData = new CompressData[_tree.CurrentBranchCount];
            int currentIndex = 0;
            
            _tree.TraverseFromRoot(data =>
            {
                if (_tree.GetNodeBy(data.nodeIndex).isLeaf)
                {
                    return ExecutionSignal.Continue;
                }

                for (int i = 0; i < currentIndex; i++)
                {
                    if (rootInterestingBranchesCompressData[i].HasNotInterestingNodeWith(data.nodeIndex))
                    {
                        return ExecutionSignal.Continue;
                    }
                }

                rootInterestingBranchesCompressData[currentIndex++] = GetCompressDataFor(data.nodeIndex);
                return ExecutionSignal.Continue;
            });

            for (int i = 0; i < currentIndex; i++)
            {
                Compress(rootInterestingBranchesCompressData[i]);
            }
        }
        
        private void Compress(int nodeIndex)
        {
            Assert.IsTrue(_tree.ContainsNodeWith(nodeIndex));

            if (_tree.GetNodeBy(nodeIndex).isLeaf)
            {
                return;
            }

            Compress(GetCompressDataFor(nodeIndex));
        }
        
        private void Compress(CompressData data)
        {
            Assert.IsNotNull(data.notInterestingNodeIndexes);
            
            if (data.notInterestingNodeIndexes.Length == 0)
            {
                return;
            }
            
            for (int i = 0; i < data.notInterestingNodeIndexes.Length; i++)
            {
                _tree.DeleteChildrenNodesFor(data.notInterestingNodeIndexes[i]);
            }

            Assert.IsTrue(data.firstInterestingNodeIndex != data.nodeIndex);
            _tree.LinkChildrenNodesTo(data.nodeIndex, data.firstInterestingNodeIndex);
        }
        
        private CompressData GetCompressDataFor(int branchIndex)
        {
            Assert.IsTrue(_tree.ContainsNodeWith(branchIndex));
            Assert.IsFalse(_tree.GetNodeBy(branchIndex).isLeaf);

            int[] notInterestingNodeIndexes = new int[_tree.CurrentBranchCount];
            int currentNotInterestingNodeIndex = 0;

            int firstInterestingNodeIndex = branchIndex, currentParentIndex = Null, branchAmongChildrenCount = 0, childrenObjectCount = 0;
            bool isNotInterestingNode;

            _tree.TraverseFrom(branchIndex, data =>
            {
                if (data.isParentChanged)
                {
                    currentParentIndex = data.node.parentIndex;
                    branchAmongChildrenCount = childrenObjectCount = 0;
                }

                if (data.node.isLeaf == false)
                {
                    branchAmongChildrenCount++;
                }

                childrenObjectCount += data.node.objectsCount;

                if (data.isLastChild)
                {
                    bool isNotInterestingNode = childrenObjectCount == 0 && branchAmongChildrenCount == 1;
                    
                    if (isNotInterestingNode)
                    {
                        Assert.IsTrue(currentParentIndex != Null);
                        notInterestingNodeIndexes[currentNotInterestingNodeIndex++] = currentParentIndex;
                    }
                    else
                    {
                        firstInterestingNodeIndex = _tree.GetNodeBy(currentParentIndex).firstChildIndex;
                        return ExecutionSignal.Stop;
                    }
                }

                return ExecutionSignal.Continue;
            }, needTraverseForStartNode: false);
            
            int[] oversizedNotInterestingNodeIndexes = notInterestingNodeIndexes;
            notInterestingNodeIndexes = new int[currentNotInterestingNodeIndex];

            for (int i = 0; i < notInterestingNodeIndexes.Length; i++)
            {
                notInterestingNodeIndexes[i] = oversizedNotInterestingNodeIndexes[(notInterestingNodeIndexes.Length - 1) - i];
            }
            
            return new CompressData
            {
                nodeIndex = branchIndex,
                firstInterestingNodeIndex = firstInterestingNodeIndex,
                notInterestingNodeIndexes = notInterestingNodeIndexes
            };
        }
        
        private int Decompress(int branchIndex, TBounds objBounds)
        {
            Assert.IsTrue(_tree.ContainsNodeWith(branchIndex));
            Assert.IsFalse(_tree.GetNodeBy(branchIndex).isLeaf);

            int lastBranchIndex = branchIndex, targetLeafIndex = Null;
            TBounds firstChildNodeBounds = _tree.GetNodeBy(_tree.GetNodeBy(branchIndex).firstChildIndex).bounds;
            bool isChildFoundForFirstChildNode, isChildFoundForTargetObject;
            
            do
            {
                isChildFoundForFirstChildNode = isChildFoundForTargetObject = false;
                int[] childrenIndexes = _tree.Split(lastBranchIndex);

                for (int i = 0; i < childrenIndexes.Length; i++)
                {
                    if (_tree.GetNodeBy(childrenIndexes[i]).bounds.Contains(firstChildNodeBounds))
                    {
                        _tree.LinkChildrenNodesTo(childrenIndexes[i], _tree.GetNodeBy(lastBranchIndex).firstChildIndex);
                        _tree.LinkChildrenNodesTo(lastBranchIndex, childrenIndexes[0]);
                        
                        lastBranchIndex = childrenIndexes[i];
                        isChildFoundForFirstChildNode = true;
                    }

                    if (_tree.GetNodeBy(childrenIndexes[i]).bounds.Intersects(objBounds) && lastBranchIndex != childrenIndexes[i])
                    {
                        targetLeafIndex = childrenIndexes[i];
                        isChildFoundForTargetObject = true;
                    }

                    if (isChildFoundForFirstChildNode && isChildFoundForTargetObject)
                    {
                        break;
                    }
                }
            }
            while (_tree.GetNodeBy(lastBranchIndex).isLeaf == false && targetLeafIndex == Null);
            
            Assert.IsFalse(targetLeafIndex == Null);
            return targetLeafIndex;
        }
    }
}
