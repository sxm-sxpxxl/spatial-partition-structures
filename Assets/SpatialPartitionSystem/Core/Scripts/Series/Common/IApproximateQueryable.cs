using System.Collections.Generic;

namespace SpatialPartitionSystem.Core.Series
{
    internal interface IApproximateQueryable<TObject> where TObject : class
    {
        public IReadOnlyList<TObject> ApproximateQuery(AABB2D queryBounds, float epsilon);
    }
}