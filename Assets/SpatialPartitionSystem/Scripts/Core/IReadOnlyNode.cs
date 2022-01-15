using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public interface IReadOnlyNode<TObject> where TObject : class, ISpatialObject
    {
        IReadOnlyList<IReadOnlyNode<TObject>> Childrens { get; }
        IReadOnlyList<TObject> Objects { get; }
        Bounds Bounds { get; }
    }
}
