using System.Collections.Generic;

namespace SpatialPartitionSystem.Core.Series
{
    internal interface IQueryable<TObject> where TObject : class
    {
        IReadOnlyList<TObject> Query(AABB2D queryBounds);
    }
}