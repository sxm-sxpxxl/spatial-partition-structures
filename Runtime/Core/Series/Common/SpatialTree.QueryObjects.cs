using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Sxm.SpatialPartitionStructures.Core.Series
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
            _tempIndexes.Clear();
            _tempBounds = queryBounds;

            TraverseFromRoot(data =>
            {
                if (data.node.isLeaf && _tempBounds.Intersects(data.node.bounds))
                {
                    _tempIndexes.Add(data.nodeIndex);
                }

                return ExecutionSignal.Continue;
            });
            
            for (int i = 0; i < _tempIndexes.Count; i++)
            {
                AddLeafObjects(_tempIndexes[i], _queryObjects, new AddObjectRequest
                {
                    queryBounds = _tempBounds,
                    needIntersectionCheck = true
                });
            }

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
            
            _tempIndexes.Clear();
            
            if (_nodes[nodeIndex].isLeaf)
            {
                AddLeafObjects(nodeIndex, objects, new AddObjectRequest { needIntersectionCheck = false });
                return;
            }
            
            TraverseFrom(nodeIndex, data =>
            {
                if (data.node.isLeaf)
                {
                    _tempIndexes.Add(data.nodeIndex);
                }
                return ExecutionSignal.Continue;
            }, needTraverseForStartNode: false);
            
            for (int i = 0; i < _tempIndexes.Count; i++)
            {
                AddLeafObjects(_tempIndexes[i], objects, new AddObjectRequest { needIntersectionCheck = false });
            }
        }
    }
}
