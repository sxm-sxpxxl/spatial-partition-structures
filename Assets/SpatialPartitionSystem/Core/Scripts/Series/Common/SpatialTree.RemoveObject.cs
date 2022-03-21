using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal partial class SpatialTree<TObject> : ISpatialTree<TObject> where TObject : class
    {
        public bool TryRemove(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            
            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsFalse(linkedLeafIndex == Null);
            
            Remove(objectIndex);
            return true;
        }
        
        private void Remove(int objectIndex)
        {
            Assert.IsTrue(objectIndex >= 0 && objectIndex < _objects.Capacity);

            int leafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(leafIndex >= 0 && leafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[leafIndex].isLeaf);
            
            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(leafIndex);

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                if (_objectPointers[unlinkedPointersIndexes[i]].objectIndex == objectIndex)
                {
                    DeleteObject(unlinkedPointersIndexes[i]);
                }
                else
                {
                    LinkObjectPointerTo(leafIndex, unlinkedPointersIndexes[i]);
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
