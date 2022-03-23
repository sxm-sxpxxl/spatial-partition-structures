using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series.Trees
{
    public class BaseTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>, IQueryable<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        private readonly SpatialTree<TObject, TBounds, TVector> _tree;

        public BaseTree(Dimension dimension, TBounds rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _tree = new SpatialTree<TObject, TBounds, TVector>(dimension, rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
        }
        
        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            _tree.DebugDraw(relativeTransform, isPlaymodeOnly);
        }

        public bool TryAdd(TObject obj, TBounds objBounds, out int objectIndex)
        {
            return _tree.TryAdd(obj, objBounds, out objectIndex);
        }

        public void Remove(int objectIndex) => _tree.Remove(objectIndex);

        public int Update(int objectIndex, TBounds updatedObjBounds) => _tree.Update(objectIndex, updatedObjBounds);

        public void CleanUp() => _tree.CleanUp();

        public IReadOnlyList<TObject> Query(TBounds queryBounds) => _tree.Query(queryBounds);
    }
}
