using System;
using System.Collections.Generic;
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

        private readonly List<Bounds2DObject> list = new List<Bounds2DObject>(capacity: 10);

        private Quadtree<Transform> _quadtree;
        
        private void OnDrawGizmos()
        {
            if (_quadtree == null)
            {
                return;
            }

            _quadtree.DebugDraw(transform);
        }

        private void Start()
        {
            _quadtree = new Quadtree<Transform>(GetComponent<Bounds2DObject>().Bounds, 1, 4, 8);

            _quadtree.TryAdd(aObject.transform, aObject.Bounds);
            list.Add(aObject);
            
            _quadtree.TryAdd(bObject.transform, bObject.Bounds);
            list.Add(bObject);
            
            _quadtree.TryAdd(cObject.transform, cObject.Bounds);
            list.Add(cObject);
            
            _quadtree.TryAdd(dObject.transform, dObject.Bounds);
            list.Add(dObject);
            
            _quadtree.TryAdd(eObject.transform, eObject.Bounds);
            list.Add(eObject);
            
            _quadtree.TryAdd(fObject.transform, fObject.Bounds);
            list.Add(fObject);
            
            _quadtree.TryAdd(gObject.transform, gObject.Bounds);
            list.Add(gObject);

            // _quadtree.TryRemove(aObject.transform, aObject.Bounds);
            // _quadtree.TryRemove(bObject.transform, bObject.Bounds);
            //
            _quadtree.TryAdd(hObject.transform, hObject.Bounds);
            list.Add(hObject);
            //
            // _quadtree.TryRemove(fObject.transform, fObject.Bounds);
            // _quadtree.TryRemove(gObject.transform, gObject.Bounds);
        }

        private void Update()
        {
            for (int i = 0; i < list.Count; i++)
            {
                _quadtree.Update(list[i].transform, list[i].Bounds);
            }
            
            _quadtree.CleanUp();
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