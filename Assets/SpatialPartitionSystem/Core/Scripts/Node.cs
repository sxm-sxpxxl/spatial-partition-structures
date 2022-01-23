using System.Collections.Generic;
using System.Linq;

namespace SpatialPartitionSystem.Core
{
    internal sealed class Node<TObject, TBounds> : IReadOnlyNode<TObject, TBounds>
        where TObject : class, ISpatialObject<TBounds>
        where TBounds : struct
    {
        public Node<TObject, TBounds>[] Childrens;
        public readonly List<TObject> Objects;
        public TBounds Bounds;

        public bool IsLeaf => Childrens == null || Childrens.Length == 0;

        public Node(TBounds bounds, int maxObjects)
        {
            Childrens = null;
            Objects = new List<TObject>(capacity: maxObjects);
            Bounds = bounds;
        }
        
        IReadOnlyList<IReadOnlyNode<TObject, TBounds>> IReadOnlyNode<TObject, TBounds>.Childrens => Childrens?.ToList();

        IReadOnlyList<TObject> IReadOnlyNode<TObject, TBounds>.Objects => Objects;

        TBounds IReadOnlyNode<TObject, TBounds>.Bounds => Bounds;
    }
}
