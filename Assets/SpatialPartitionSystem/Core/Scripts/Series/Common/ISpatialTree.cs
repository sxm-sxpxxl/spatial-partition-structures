using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    internal interface ISpatialTree<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false);
        bool TryAdd(TObject obj, TBounds objBounds, out int objectIndex);
        void Remove(int objectIndex);
        int Update(int objectIndex, TBounds updatedObjBounds);
        void CleanUp();
    }
}
