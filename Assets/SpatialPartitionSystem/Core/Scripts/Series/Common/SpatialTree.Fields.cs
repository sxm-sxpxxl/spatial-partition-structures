using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    internal partial class SpatialTree<TObject> : ISpatialTree<TObject> where TObject : class
    {
        internal const int RootIndex = 0, Null = -1;
        
        private const int MaxPossibleDepth = 8;
        
        private static int _cachedEqualNodeIndex;
        private static AABB2D _cachedEqualBounds;
        private static List<int> _cachedLeafIndexes = new List<int>(capacity: 1000);
        
        private readonly FreeList<Node> _nodes;
        private readonly FreeList<ObjectPointer> _objectPointers;
        private readonly FreeList<NodeObject<TObject>> _objects;
        
        private readonly int _maxLeafObjects, _maxDepth;
        
        private readonly int[] _branchIndexes;
        private MissingObjectData[] _missingObjects;
        private readonly List<TObject> _queryObjects;

        internal int NodesCapacity => _nodes.Capacity;

        public SpatialTree(AABB2D rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _maxLeafObjects = maxLeafObjects;
            _maxDepth = Mathf.Min(maxDepth, MaxPossibleDepth);

            int maxNodesCount = GetMaxNodesCountFor(_maxDepth);
            int maxBranchCount = maxNodesCount / 4;
        
            _nodes = new FreeList<Node>(maxNodesCount);
            _objectPointers = new FreeList<ObjectPointer>(initialObjectsCapacity);
            _objects = new FreeList<NodeObject<TObject>>(initialObjectsCapacity);
            _branchIndexes = ArrayUtility.CreateArray(capacity: maxBranchCount, defaultValue: Null);
            _missingObjects = new MissingObjectData[initialObjectsCapacity];
            _queryObjects = new List<TObject>(capacity: initialObjectsCapacity);
            
            _nodes.Add(new Node
            {
                parentIndex = Null,
                firstChildIndex = Null,
                objectsCount = 0,
                isLeaf = true,
                depth = 0,
                bounds = rootBounds
            }, out _);
        }

        public bool ContainsNodeWith(int nodeIndex) => _nodes.Contains(nodeIndex);

        public bool ContainsObjectWith(int objectIndex) => _objects.Contains(objectIndex);
        
        public Node GetNodeBy(int nodeIndex) => _nodes[nodeIndex];

        public NodeObject<TObject> GetNodeObjectBy(int objectIndex) => _objects[objectIndex];
        
        private static int GetMaxNodesCountFor(int maxDepth)
        {
            int maxNodesCount = 0;

            for (int i = 0; i <= maxDepth; i++)
            {
                maxNodesCount += (int) Mathf.Pow(4, i);
            }
            
            return maxNodesCount;
        }
    }
}
