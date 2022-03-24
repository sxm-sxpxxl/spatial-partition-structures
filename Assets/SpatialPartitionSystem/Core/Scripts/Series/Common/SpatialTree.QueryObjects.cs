using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        internal struct AddObjectRequest
        {
            public TBounds queryBounds;
            public bool needIntersectionCheck;
        }
        
        public IReadOnlyList<TObject> Query(TBounds queryBounds)
        {
            _queryObjects.Clear();

            TraverseFromRoot(data =>
            {
                if (_nodes[data.nodeIndex].isLeaf == false || queryBounds.Intersects(_nodes[data.nodeIndex].bounds) == false)
                {
                    return ExecutionSignal.Continue;
                }

                AddLeafObjects(data.nodeIndex, _queryObjects, new AddObjectRequest
                {
                    queryBounds = queryBounds,
                    needIntersectionCheck = true
                });
                return ExecutionSignal.Continue;
            });

            return _queryObjects;
        }
        
        internal void AddLeafObjects(int leafIndex, ICollection<TObject> objects, AddObjectRequest request)
        {
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsNotNull(objects);
            
            var leaf = _nodes[leafIndex];
            Assert.IsTrue(leaf.isLeaf);

            if (leaf.objectsCount == 0)
            {
                return;
            }

            NodeObject<TObject, TBounds, TVector> currentNodeObject;
            int currentObjectIndex = leaf.firstChildIndex;

            do
            {
                currentNodeObject = _objects[currentObjectIndex];

                if (request.needIntersectionCheck == false || request.queryBounds.Intersects(currentNodeObject.bounds))
                {
                    objects.Add(currentNodeObject.target);
                }
                
                currentObjectIndex = currentNodeObject.nextObjectIndex;
            }
            while (currentNodeObject.nextObjectIndex != Null);
        }

        internal void DeepAddNodeObjects(int nodeIndex, ICollection<TObject> objects)
        {
            Assert.IsTrue(_nodes.Contains(nodeIndex));
            Assert.IsNotNull(objects);
            
            if (_nodes[nodeIndex].isLeaf)
            {
                AddLeafObjects(nodeIndex, objects, new AddObjectRequest { needIntersectionCheck = false });
                return;
            }
            
            TraverseFrom(nodeIndex, data =>
            {
                if (data.node.isLeaf)
                {
                    _cachedLeafIndexes.Add(data.nodeIndex);
                }
                return ExecutionSignal.Continue;
            }, needTraverseForStartNode: false);
            
            for (int i = 0; i < _cachedLeafIndexes.Count; i++)
            {
                AddLeafObjects(_cachedLeafIndexes[i], objects, new AddObjectRequest { needIntersectionCheck = false });
            }
            
            _cachedLeafIndexes.Clear();
        }
    }
}
