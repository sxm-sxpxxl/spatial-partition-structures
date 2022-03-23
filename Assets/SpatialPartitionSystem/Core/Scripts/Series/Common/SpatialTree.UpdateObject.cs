using System;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private struct MissingObjectData
        {
            public bool isMissing;
            public NodeObject<TObject, TBounds, TVector> nodeObject;

            public void Set(NodeObject<TObject, TBounds, TVector> value)
            {
                isMissing = true;
                nodeObject = value;
            }

            public void Reset()
            {
                isMissing = false;
                nodeObject = default;
            }
        }
        
        public int Update(int objectIndex, TBounds updatedObjBounds)
        {
            return Update(objectIndex, updatedObjBounds, AddObjectToLeaf);
        }

        public int Update(int objectIndex, TBounds updatedObjBounds, Func<int, TObject, TBounds, int> addObjectAction)
        {
            int newObjectIndex;
            
            if (IsObjectMissing(objectIndex))
            {
                if (TryAdd(_missingObjects[objectIndex].nodeObject.target, updatedObjBounds, addObjectAction, out newObjectIndex) == false)
                {
                    return objectIndex;
                }
                
                _missingObjects[objectIndex].Reset();
                return newObjectIndex;
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);

            if (_nodes[linkedLeafIndex].bounds.Intersects(updatedObjBounds))
            {
                var nodeObject = _objects[objectIndex];
                nodeObject.bounds = updatedObjBounds;
                _objects[objectIndex] = nodeObject;
                
                return objectIndex;
            }

            var removedNodeObject = _objects[objectIndex];
            Remove(objectIndex);

            if (TryAdd(removedNodeObject.target, updatedObjBounds, addObjectAction, out newObjectIndex) == false)
            {
                _missingObjects[objectIndex].Set(removedNodeObject);
                return objectIndex;
            }

            return newObjectIndex;
        }
        
        private bool IsObjectMissing(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            if (objectIndex >= _missingObjects.Length)
            {
                var newArray = new MissingObjectData[_objects.Capacity];
                _missingObjects.CopyTo(newArray, 0);
                _missingObjects = newArray;
            }

            return _missingObjects[objectIndex].isMissing;
        }
    }
}
