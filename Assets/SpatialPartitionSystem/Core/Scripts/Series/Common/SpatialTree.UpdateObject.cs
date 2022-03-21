using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal partial class SpatialTree<TObject> : ISpatialTree<TObject> where TObject : class
    {
        private struct MissingObjectData
        {
            public bool isMissing;
            public TObject obj;
        }
        
        public int Update(int objectIndex, AABB2D updatedObjBounds)
        {
            int newObjectIndex;
            
            if (IsObjectMissing(objectIndex))
            {
                if (TryAdd(_missingObjects[objectIndex].obj, updatedObjBounds, out newObjectIndex) == false)
                {
                    return objectIndex;
                }
                
                _missingObjects[objectIndex].isMissing = false;
                _missingObjects[objectIndex].obj = null;
                
                return newObjectIndex;
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);

            if (_nodes[linkedLeafIndex].bounds.Intersects(updatedObjBounds))
            {
                NodeObject<TObject> nodeObject = _objects[objectIndex];
                nodeObject.bounds = updatedObjBounds;
                
                _objects[objectIndex] = nodeObject;
                return objectIndex;
            }

            TObject obj = _objects[objectIndex].target;
            Remove(objectIndex);

            if (TryAdd(obj, updatedObjBounds, out newObjectIndex) == false)
            {
                _missingObjects[objectIndex].isMissing = true;
                _missingObjects[objectIndex].obj = obj;
                
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
