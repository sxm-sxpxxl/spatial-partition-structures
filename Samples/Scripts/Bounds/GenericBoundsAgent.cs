using UnityEngine;
using Sxm.SpatialPartitionStructures.Core.Series;

namespace Sxm.SpatialPartitionStructures.Sample
{
    public abstract class GenericBoundsAgent<TBounds, TVector> : BoundsAgent
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public abstract TBounds Bounds { get; }
    }
}
