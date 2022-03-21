using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series.Trees
{
    public class Quadtree<TObject> : ISpatialTree<TObject>, IQueryable<TObject>
        where TObject : class
    {
        private readonly SpatialTree<TObject> _tree;

        public Quadtree(AABB2D rootBounds, int maxLeafObjects, int maxDepth, int initialObjectsCapacity)
        {
            _tree = new SpatialTree<TObject>(rootBounds, maxLeafObjects, maxDepth, initialObjectsCapacity);
        }
        
        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            _tree.DebugDraw(relativeTransform, isPlaymodeOnly);
        }

        public bool TryAdd(TObject obj, AABB2D objBounds, out int objectIndex)
        {
            return _tree.TryAdd(obj, objBounds, out objectIndex);
        }

        public bool TryRemove(int objectIndex)
        {
            return _tree.TryRemove(objectIndex);
        }

        public int Update(int objectIndex, AABB2D updatedObjBounds)
        {
            return _tree.Update(objectIndex, updatedObjBounds);
        }

        public void CleanUp()
        {
            _tree.CleanUp();
        }

        public IReadOnlyList<TObject> Query(AABB2D queryBounds)
        {
            return _tree.Query(queryBounds);
        }
    }
}
