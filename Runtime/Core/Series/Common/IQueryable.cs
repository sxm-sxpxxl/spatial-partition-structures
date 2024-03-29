﻿using System.Collections.Generic;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal interface IQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        IReadOnlyList<TObject> Query(TBounds queryBounds);
    }
}
