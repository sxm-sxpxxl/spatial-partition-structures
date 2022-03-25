using System;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        public int Update(int objectIndex, TBounds updatedObjBounds)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            
            if (_objects[objectIndex].isMissing)
            {
                return ReaddObject(objectIndex, updatedObjBounds);
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[linkedLeafIndex].isLeaf);

            if (_nodes[linkedLeafIndex].bounds.Intersects(updatedObjBounds))
            {
                var nodeObject = _objects[objectIndex];
                nodeObject.bounds = updatedObjBounds;
                _objects[objectIndex] = nodeObject;
                
                return objectIndex;
            }

            return ReaddObject(objectIndex, updatedObjBounds);
        }

        private int ReaddObject(int objectIndex, TBounds updatedObjBounds)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));

            var nodeObject = _objects[objectIndex];
            
            int targetNodeIndex = FindTargetNodeIndex(updatedObjBounds);
            if (targetNodeIndex != Null)
            {
                Remove(objectIndex);
                return _cachedAddObjectAction.Invoke(targetNodeIndex, nodeObject.target, updatedObjBounds);
            }

            nodeObject.isMissing = true;
            _objects[objectIndex] = nodeObject;
            
            return objectIndex;
        }
    }
}
