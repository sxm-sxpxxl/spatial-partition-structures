using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    public interface IAABB<TVector> where TVector : struct
    {
        TVector Center { get; }
        TVector Extents { get; }
        TVector Size { get; }
        TVector Min { get; }
        TVector Max { get; }
        bool Equals<TBounds>(TBounds other) where TBounds : IAABB<TVector>;
        bool Contains(TVector point);
        bool Contains<TBounds>(TBounds other) where TBounds : IAABB<TVector>;
        bool Intersects<TBounds>(TBounds other) where TBounds : IAABB<TVector>;
        float IntersectionArea<TBounds>(TBounds other) where TBounds : IAABB<TVector>;
        IAABB<TVector> GetChildBoundsBy(SplitSection splitSectionIndex);
        IAABB<TVector> GetExtendedBoundsOn(float offset);
        Vector3 TransformCenter(Transform relativeTransform);
        Vector3 TransformSize(Transform relativeTransform);
    }
}
