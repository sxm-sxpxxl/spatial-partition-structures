using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public void Remove(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            if (IsObjectMissing(objectIndex))
            {
                _missingObjects[objectIndex].Reset();
                return;
            }
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[linkedLeafIndex].isLeaf);
            
            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(linkedLeafIndex);

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                if (_objectPointers[unlinkedPointersIndexes[i]].objectIndex == objectIndex)
                {
                    DeleteObject(unlinkedPointersIndexes[i]);
                }
                else
                {
                    LinkObjectPointerTo(linkedLeafIndex, unlinkedPointersIndexes[i]);
                }
            }
        }
        
        private void DeleteObject(int objectPointerIndex)
        {
            Assert.IsTrue(objectPointerIndex >= 0 && objectPointerIndex < _objectPointers.Capacity);
            
            _objects.RemoveAt(_objectPointers[objectPointerIndex].objectIndex);
            _objectPointers.RemoveAt(objectPointerIndex);
        }
    }
}
