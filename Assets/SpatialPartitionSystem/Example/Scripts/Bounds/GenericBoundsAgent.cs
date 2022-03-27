using UnityEngine;
using SpatialPartitionSystem.Core.Series;

namespace SpatialPartitionSystem.Example
{
    public abstract class GenericBoundsAgent<TBounds, TVector> : BoundsAgent
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public abstract TBounds Bounds { get; }
    }
}
