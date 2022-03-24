using System;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        public int Update(int objectIndex, TBounds updatedObjBounds) => Update(objectIndex, updatedObjBounds, AddObjectToLeaf);

        internal int Update(int objectIndex, TBounds updatedObjBounds, Func<int, TObject, TBounds, int> addObjectAction)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            Assert.IsNotNull(addObjectAction);
            
            if (IsObjectMissing(objectIndex))
            {
                return ReaddObject(objectIndex, updatedObjBounds, addObjectAction);
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

            return ReaddObject(objectIndex, updatedObjBounds, addObjectAction);
        }

        private int ReaddObject(int objectIndex, TBounds updatedObjBounds, Func<int, TObject, TBounds, int> addObjectAction)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            Assert.IsNotNull(addObjectAction);
            
            var nodeObject = _objects[objectIndex];
            
            int targetNodeIndex = FindTargetNodeIndex(updatedObjBounds);
            if (targetNodeIndex != Null)
            {
                Remove(objectIndex);
                return addObjectAction.Invoke(targetNodeIndex, nodeObject.target, updatedObjBounds);
            }

            _missingObjects[objectIndex] = true;
            return objectIndex;
        }
        
        private bool IsObjectMissing(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));

            if (objectIndex >= _missingObjects.Length)
            {
                var newArray = new bool[_objects.Capacity];
                _missingObjects.CopyTo(newArray, 0);
                _missingObjects = newArray;
            }

            return _missingObjects[objectIndex];
        }
    }
}
