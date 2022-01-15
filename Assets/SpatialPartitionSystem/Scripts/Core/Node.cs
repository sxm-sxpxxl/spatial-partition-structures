using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public sealed class Node<TObject> where TObject : class
    {
        public Node<TObject>[] Childrens;
        public readonly List<TObject> Objects;
        public Bounds Bounds;
            
        public bool IsLeaf => Childrens == null || Childrens.Length == 0;

        public Node(Bounds bounds, int maxObjects)
        {
            Childrens = null;
            Objects = new List<TObject>(capacity: maxObjects);
            Bounds = bounds;
        }
    }
}
