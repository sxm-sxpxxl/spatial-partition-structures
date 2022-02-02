using System;
using UnityEngine;

namespace SpatialPartitionSystem.Core.Series
{
    public class Test : MonoBehaviour
    {
        public Bounds2DObject aObject;
        public Bounds2DObject bObject;
        public Bounds2DObject cObject;
        public Bounds2DObject dObject;
        public Bounds2DObject eObject;
        public Bounds2DObject fObject;
        public Bounds2DObject gObject;
        public Bounds2DObject hObject;
        
        private void Start()
        {
            var quadtree = new Quadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, 2, 2, 2);

            quadtree.TryAdd(aObject.transform, aObject.Bounds);
            quadtree.TryAdd(bObject.transform, bObject.Bounds);
            quadtree.TryAdd(cObject.transform, cObject.Bounds);
            quadtree.TryAdd(dObject.transform, dObject.Bounds);
            quadtree.TryAdd(eObject.transform, eObject.Bounds);
            quadtree.TryAdd(fObject.transform, fObject.Bounds);
            quadtree.TryAdd(gObject.transform, gObject.Bounds);

            quadtree.TryRemove(aObject.transform, aObject.Bounds);
            quadtree.TryRemove(bObject.transform, bObject.Bounds);

            quadtree.TryAdd(hObject.transform, hObject.Bounds);

            quadtree.TryRemove(fObject.transform, fObject.Bounds);
            quadtree.TryRemove(gObject.transform, gObject.Bounds);
            
            quadtree.CleanUp();
        }

        private void TestFreeList()
        {
            var a = new ObjectPointer();
            a.objectIndex = 1;
            a.nextObjectPointerIndex = 1;
            
            var b = new ObjectPointer();
            b.objectIndex = 2;
            b.nextObjectPointerIndex = 2;
            
            var c = new ObjectPointer();
            c.objectIndex = 3;
            c.nextObjectPointerIndex = 3;
            
            
            var list = new FreeList<ObjectPointer>(2);

            list.Add(a, out int aIndex);
            list.Add(b, out int bIndex);

            list.RemoveAt(aIndex);
            
            list.Add(c, out int cIndex);

            ObjectPointer bPointer = list[bIndex];
            ObjectPointer cPointer = list[cIndex];
        }
    }
}