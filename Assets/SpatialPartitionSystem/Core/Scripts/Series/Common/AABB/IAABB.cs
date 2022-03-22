using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public interface IAABB<TVector> : IEquatable<IAABB<TVector>> where TVector : struct
    {
        TVector Center { get; }
        TVector Extents { get; }
        TVector Size { get; }
        TVector Min { get; }
        TVector Max { get; }
        bool Contains(TVector point);
        bool Contains(IAABB<TVector> other);
        bool Intersects(IAABB<TVector> other);
        float IntersectionArea(IAABB<TVector> other);
        IAABB<TVector> GetChildBoundsBy(SplitSection splitSectionIndex);
        IAABB<TVector> GetExtendedBoundsOn(float offset);
        Vector3 TransformCenter(Transform relativeTransform);
        Vector3 TransformSize(Transform relativeTransform);
    }
}
