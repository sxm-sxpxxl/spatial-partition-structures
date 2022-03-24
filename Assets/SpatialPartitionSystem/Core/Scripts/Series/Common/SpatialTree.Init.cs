using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
        : ISpatialTree<TObject, TBounds, TVector>, IQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        internal const int RootIndex = 0, Null = -1;
        
        private const int MaxPossibleDepth = 8, QuadtreeChildrenCount = 4, OctreeChildrenCount = 8;
        
        private static int _cachedEqualNodeIndex;
        private static TBounds _cachedEqualBounds;
        private static List<int> _cachedLeafIndexes = new List<int>(capacity: 1000);
        
        private readonly FreeList<Node<TBounds, TVector>> _nodes;
        private readonly FreeList<NodeObject<TObject, TBounds, TVector>> _objects;
        
        private readonly int _maxLeafObjects, _maxDepth, _maxChildrenCount;
        
        private readonly int[] _branchIndexes;
        private readonly List<TObject> _queryObjects;

        internal int NodesCapacity => _nodes.Capacity;
        internal int MaxChildrenCount => _maxChildrenCount;

        internal SpatialTree(Dimension dimension, TBounds rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            Assert.IsTrue(maxLeafObjects > 0);
            Assert.IsTrue(maxDepth > 0);
            Assert.IsTrue(initialObjectsCapacity > 0);
            
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = Mathf.Min(maxDepth, MaxPossibleDepth);
            _maxChildrenCount = dimension == Dimension.Two ? QuadtreeChildrenCount : OctreeChildrenCount;

            int maxNodesCount = GetMaxNodesCountFor(_maxDepth);
            int maxBranchCount = maxNodesCount / _maxChildrenCount;
        
            _nodes = new FreeList<Node<TBounds, TVector>>(maxNodesCount);
            _objects = new FreeList<NodeObject<TObject, TBounds, TVector>>(initialObjectsCapacity);
            _branchIndexes = ArrayUtility.CreateArray(capacity: maxBranchCount, defaultValue: Null);
            _queryObjects = new List<TObject>(capacity: initialObjectsCapacity);
            
            _nodes.Add(new Node<TBounds, TVector>(depth: 0, bounds: rootBounds), out _);
        }

        internal bool ContainsNodeWith(int nodeIndex) => _nodes.Contains(nodeIndex);

        internal bool ContainsObjectWith(int objectIndex) => _objects.Contains(objectIndex);
        
        internal Node<TBounds, TVector> GetNodeBy(int nodeIndex) => _nodes[nodeIndex];

        internal NodeObject<TObject, TBounds, TVector> GetNodeObjectBy(int objectIndex) => _objects[objectIndex];
        
        private int GetMaxNodesCountFor(int maxDepth)
        {
            Assert.IsTrue(_maxChildrenCount != 0);
            
            int maxNodesCount = 0;
            for (int i = 0; i <= maxDepth; i++)
            {
                maxNodesCount += (int) Mathf.Pow(_maxChildrenCount, i);
            }
            
            return maxNodesCount;
        }
    }
}
