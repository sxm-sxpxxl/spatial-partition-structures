using System.Collections.Generic;

namespace SpatialPartitionSystem.Core
{
    public interface IReadOnlyNode<TObject, TBounds>
        where TObject : class, ISpatialObject<TBounds>
        where TBounds : struct
    {
        IReadOnlyList<IReadOnlyNode<TObject, TBounds>> Childrens { get; }
        IReadOnlyList<TObject> Objects { get; }
        TBounds Bounds { get; }
    }
}
