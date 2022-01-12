using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionSystem
{
    public class Node<TObject> where TObject : class
    {
        public Node<TObject>[] childrens;
        public List<TObject> objects;
        public Bounds bounds;
            
        public bool IsLeaf => childrens == null || childrens.Length == 0;

        public Node(Bounds bounds, int maxObjects)
        {
            childrens = null;
            objects = new List<TObject>(capacity: maxObjects);
            this.bounds = bounds;
        }
    }
}
