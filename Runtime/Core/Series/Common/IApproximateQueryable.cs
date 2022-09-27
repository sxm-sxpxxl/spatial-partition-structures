using System.Collections.Generic;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal interface IApproximateQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public IReadOnlyList<TObject> ApproximateQuery(TBounds queryBounds, float epsilon);
    }
}
