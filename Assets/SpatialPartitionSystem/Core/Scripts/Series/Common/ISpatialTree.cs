using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    internal interface ISpatialTree<TObject> where TObject : class
    {
        void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false);
        bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex);
        bool TryRemove(int objectIndex);
        int Update(int objectIndex, AABB2D updatedObjBounds);
        void CleanUp();
    }
}
