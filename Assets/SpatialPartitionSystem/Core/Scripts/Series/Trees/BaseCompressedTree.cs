using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    public class BaseCompressedTree<TObject, TBounds, TVector>
        : ISpatialTree<TObject, TBounds, TVector>, IQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private const int Null = SpatialTree<TObject, TBounds, TVector>.Null;
        
        private readonly SpatialTree<TObject, TBounds, TVector> _tree;

        internal int NodesCapacity => _tree.NodesCapacity;
        internal int MaxChildrenCount => _tree.MaxChildrenCount;
        
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
            Assert.IsTrue(maxLeafObjects > 0);
            Assert.IsTrue(maxDepth > 0);
            Assert.IsTrue(initialObjectsCapacity > 0);
            _tree = new SpatialTree<TObject, TBounds, TVector>(
                dimension,
                rootBounds,
                maxLeafObjects,
                maxDepth,
                initialObjectsCapacity
            ).Override(addObjectAction: AddToCompressedNode);
        }

        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false) => _tree.DebugDraw(relativeTransform, isPlaymodeOnly);

        public bool TryAdd(TObject obj, TBounds objBounds, out int objectIndex) => _tree.TryAdd(obj, objBounds, out objectIndex);

        public void Remove(int objectIndex) => _tree.Remove(objectIndex);
        
        public int Update(int objectIndex, TBounds updatedObjBounds) => _tree.Update(objectIndex, updatedObjBounds);

        public void CleanUp()
        {
            if (_tree.CurrentBranchCount == 0)
            {
                return;
            }

            DeepCompress();
            _tree.DeepCleanUp();
        }

        public IReadOnlyList<TObject> Query(TBounds queryBounds) => _tree.Query(queryBounds);
        
        internal int AddToCompressedNode(int nodeIndex, TObject obj, TBounds objBounds)
        {
            Assert.IsTrue(_tree.ContainsNodeWith(nodeIndex));
            Assert.IsNotNull(obj);

            int objectIndex;
            
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

            return objectIndex;
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
            Assert.IsTrue(_tree.GetNodeBy(branchIndex).isLeaf == false);

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
                    isNotInterestingNode = childrenObjectCount == 0 && branchAmongChildrenCount == 1;
                    
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
            Assert.IsTrue(_tree.GetNodeBy(branchIndex).isLeaf == false);

            int currentBranchIndex = branchIndex, lastBranchIndex, targetLeafIndex = Null;
            TBounds firstChildNodeBounds = _tree.GetNodeBy(_tree.GetNodeBy(branchIndex).firstChildIndex).bounds;
            bool isChildFoundForFirstChildNode, isChildFoundForTargetObject;
            
            do
            {
                lastBranchIndex = currentBranchIndex;
                isChildFoundForFirstChildNode = isChildFoundForTargetObject = false;
                
                int[] childrenIndexes = _tree.Split(currentBranchIndex);

                for (int i = 0; i < childrenIndexes.Length; i++)
                {
                    if (_tree.GetNodeBy(childrenIndexes[i]).bounds.Contains(firstChildNodeBounds))
                    {
                        _tree.LinkChildrenNodesTo(childrenIndexes[i], _tree.GetNodeBy(currentBranchIndex).firstChildIndex);
                        _tree.LinkChildrenNodesTo(currentBranchIndex, childrenIndexes[0]);

                        currentBranchIndex = childrenIndexes[i];
                        isChildFoundForFirstChildNode = true;
                    }

                    if (_tree.GetNodeBy(childrenIndexes[i]).bounds.Intersects(objBounds) && currentBranchIndex != childrenIndexes[i])
                    {
                        targetLeafIndex = childrenIndexes[i];
                        isChildFoundForTargetObject = true;
                    }

                    if (isChildFoundForFirstChildNode && isChildFoundForTargetObject)
                    {
                        break;
                    }
                }

                Assert.IsTrue(currentBranchIndex != lastBranchIndex);
            }
            while (_tree.GetNodeBy(currentBranchIndex).isLeaf == false && targetLeafIndex == Null);
            
            Assert.IsTrue(targetLeafIndex != Null);
            return targetLeafIndex;
        }
    }
}
