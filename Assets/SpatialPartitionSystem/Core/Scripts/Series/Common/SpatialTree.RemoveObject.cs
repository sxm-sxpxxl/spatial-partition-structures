using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector>
    {
        public void Remove(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));

            int linkedLeafIndex = _objects[objectIndex].leafIndex;
            Assert.IsTrue(linkedLeafIndex >= 0 && linkedLeafIndex < _nodes.Capacity);
            Assert.IsTrue(_nodes[linkedLeafIndex].isLeaf);
            
            int[] unlinkedPointersIndexes = UnlinkObjectPointersFrom(linkedLeafIndex);

            for (int i = 0; i < unlinkedPointersIndexes.Length; i++)
            {
                if (unlinkedPointersIndexes[i] == objectIndex)
                {
                    DeleteObject(unlinkedPointersIndexes[i]);
                }
                else
                {
                    LinkObjectTo(linkedLeafIndex, unlinkedPointersIndexes[i]);
                }
            }
        }
        
        private void DeleteObject(int objectIndex)
        {
            Assert.IsTrue(_objects.Contains(objectIndex));
            _objects.RemoveAt(objectIndex);
        }
    }
}
