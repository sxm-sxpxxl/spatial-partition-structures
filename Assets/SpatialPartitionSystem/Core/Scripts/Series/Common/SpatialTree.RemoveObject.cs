using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        public void Remove(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));

            if (IsObjectMissing(objectIndex))
            {
                _missingObjects[objectIndex] = false;
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
            Assert.IsTrue(_objectPointers.Contains(objectPointerIndex));
            
            _objects.RemoveAt(_objectPointers[objectPointerIndex].objectIndex);
            _objectPointers.RemoveAt(objectPointerIndex);
        }
    }
}
