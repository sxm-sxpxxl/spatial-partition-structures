using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed class FreeList<TObject>
    {
        private TObject[] _items;
        private readonly Queue<int> _freeIndexes;
        private int _nextItemIndex;

        public int Capacity => _items.Length;
        public int Count { get; private set; } = 0;

        private bool HasFreeItem => _freeIndexes.Count != 0;

        internal TObject this[int index]
        {
            get
            {
                Assert.IsTrue(index >= 0 && index < Capacity);
                return _items[index];
            }
            set
            {
                Assert.IsTrue(index >= 0 && index < Capacity);
                _items[index] = value;
            }
        }
        
        internal FreeList(int capacity = 128)
        {
            _items = new TObject[capacity];
            _freeIndexes = new Queue<int>(capacity);
            _nextItemIndex = 0;
        }

        internal void Add(TObject item, out int itemIndex)
        {
            if (HasFreeItem)
            {
                int freeItemIndex = _freeIndexes.Dequeue();
                _items[freeItemIndex] = item;
                itemIndex = freeItemIndex;
            }
            else
            {
                if (_nextItemIndex == _items.Length)
                {
                    TObject[] newArray = new TObject[2 * _items.Length];
                    _items.CopyTo(newArray, 0);
                    _items = newArray;
                }
                
                _items[_nextItemIndex] = item;
                itemIndex = _nextItemIndex;
                _nextItemIndex++;
            }
            
            Count++;
        }

        internal void RemoveAt(int index)
        {
            Assert.IsTrue(index >= 0 && index < Capacity);
            _freeIndexes.Enqueue(index);
            Count--;
        }

        internal bool Contains(int index)
        {
            return index >= 0 && index < Capacity &&
                   index < _nextItemIndex && _freeIndexes.Contains(index) == false;
        }
    }
}
