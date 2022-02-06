using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed class FreeList<T>
    {
        private T[] _items;
        private Queue<int> _freeIndexes;
        private int _nextItemIndex;

        public int Capacity => _items.Length;
        public int Count { get; private set; } = 0;

        private bool HasFreeItem => _freeIndexes.Count != 0;

        public T this[int index]
        {
            get
            {
                Assert.IsTrue(index >= 0 && index < _items.Length);
                return _items[index];
            }
            set
            {
                Assert.IsTrue(index >= 0 && index < _items.Length);
                _items[index] = value;
            }
        }
        
        public FreeList(int capacity = 128)
        {
            _items = new T[capacity];
            _freeIndexes = new Queue<int>(capacity);
            _nextItemIndex = 0;
        }

        public void Add(T item, out int itemIndex)
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
                    T[] newArray = new T[2 * _items.Length];
                    _items.CopyTo(newArray, 0);
                    _items = newArray;
                }
                
                _items[_nextItemIndex] = item;
                itemIndex = _nextItemIndex;
                _nextItemIndex++;
            }
            
            Count++;
        }

        public void RemoveAt(int index)
        {
            _freeIndexes.Enqueue(index);
            Count--;
        }
    }
}
