using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public sealed class Node<TObject> : IReadOnlyNode<TObject>
        where TObject : class, ISpatialObject
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
        
        IReadOnlyList<IReadOnlyNode<TObject>> IReadOnlyNode<TObject>.Childrens => Childrens?.ToList();

        IReadOnlyList<TObject> IReadOnlyNode<TObject>.Objects => Objects;

        Bounds IReadOnlyNode<TObject>.Bounds => Bounds;
    }
}
