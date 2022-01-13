using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem
{
    public sealed class Quadtree<TObject> where TObject : class, ISpatialObject
    {
        private readonly Dictionary<int, Func<Vector3, Vector3, Vector3>> quadrantOrientationMap =
            new Dictionary<int, Func<Vector3, Vector3, Vector3>>
            {
                { 0, (min, max) => new Vector3(max.x, max.y) },
                { 1, (min, max) => new Vector3(min.x, max.y) },
                { 2, (min, max) => new Vector3(min.x, min.y) },
                { 3, (min, max) => new Vector3(max.x, min.y) }
            };
        
        private readonly int maxObjects = 0, maxDepth = 0;
        private readonly Node<TObject> root;

        /// <summary>
        /// Constructs a new tree with given settings.
        /// </summary>
        /// <param name="bounds">The bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        public Quadtree(Bounds bounds, int maxObjects, int maxDepth)
        {
            root = new Node<TObject>(bounds, maxObjects);
            this.maxObjects = maxObjects;
            this.maxDepth = maxDepth;
        }

        /// <summary>
        /// Draws the bounds of the tree using Debug.DrawLine if playmode only setting is true or Gizmos.DrawLine if not.
        /// </summary>
        /// <param name="isPlaymodeOnly">The playmode only setting.</param>
        public void DebugDraw(bool isPlaymodeOnly = false)
        {
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();

            void DrawNestedBounds(Node<TObject> node)
            {
                var busyColor = node.objects.Count == 0
                    ? Color.green
                    : node.objects.Count >= maxObjects
                        ? Color.red
                        : Color.blue;

                drawer.SetColor(busyColor);
                drawer.DrawWireCube(node.bounds.center, node.bounds.size);

                if (node.childrens == null)
                {
                    return;
                }

                foreach (var child in node.childrens)
                {
                    DrawNestedBounds(child);
                }
            }

            DrawNestedBounds(root);
        }

        /// <summary>
        /// Tries to add the object to the tree.
        /// If the object doesn't intersect with the bounds of the tree then the object won't be added.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>true - if the object is added; false - if not.</returns>
        public bool TryAdd(TObject obj)
        {
            Assert.IsNotNull(obj);
            return TryAdd(obj, root, 0);
        }

        /// <summary>
        /// Tries to remove the object from the tree.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true - if the object is removed; false - if not.</returns>
        public bool TryRemove(TObject obj)
        {
            Assert.IsNotNull(obj);
            return TryRemove(obj, root);
        }

        /// <summary>
        /// Clears the whole tree.
        /// </summary>
        public void Clear()
        {
            root.childrens = null;
            root.objects.Clear();
        }

        /// <summary>
        /// Updates the object's location in the tree if its bounds have changed.
        /// </summary>
        /// <param name="obj">The updated object.</param>
        public void Update(TObject obj)
        {
            Assert.IsNotNull(obj);

            if (TryGetSmallestNodeRelativeFor(
                obj,
                root,
                null,
                out Node<TObject> smallestNode,
                out Node<TObject> parentSmallestNode
            ) == false)
            {
                TryAdd(obj, root, 0);
                return;
            }
            
            if (smallestNode.bounds.Intersects(obj.Bounds) == false)
            {
                TryRemove(obj, smallestNode);
                TryAdd(obj, root, 0);
            }
            else
            {
                if (parentSmallestNode != null)
                {
                    TryMerge(parentSmallestNode);
                }
            }
        }

        /// <summary>
        /// Whether the object is part of the tree or not.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Contains(TObject obj)
        {
            return TryGetSmallestNodeRelativeFor(obj, root, null, out _, out _);
        }

        /// <summary>
        /// Whether the bounds are completely inside the tree or not.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool Contains(Bounds bounds)
        {
            return root.bounds.Contains(bounds.min) && root.bounds.Contains(bounds.max);
        }

        /// <summary>
        /// Returns an enumerable which iterates over all objects overlapping the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to check.</param>
        /// <param name="maxQueryObjects">The maximum number of query objects for more efficient memory allocation for the collection.</param>
        /// <returns></returns>
        public IEnumerable<TObject> Query(Bounds bounds, int maxQueryObjects = 1000)
        {
            return Query(root, bounds, maxQueryObjects);
        }

        private IEnumerable<TObject> Query(Node<TObject> node, Bounds queryBounds, int maxQueryObjects)
        {
            if (node.bounds.Intersects(queryBounds) == false)
            {
                return null;
            }
            
            var queryObjects = new List<TObject>(capacity: maxQueryObjects);
            
            if (node.IsLeaf)
            {
                for (int i = 0; i < node.objects.Count; i++)
                {
                    if (queryBounds.Intersects(node.objects[i].Bounds))
                    {
                        queryObjects.Add(node.objects[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < node.childrens.Length; i++)
                {
                    var childQueryObjects = Query(node.childrens[i], queryBounds, (int) 0.5f * maxQueryObjects);
                    
                    if (childQueryObjects == null)
                    {
                        continue;
                    }
                        
                    queryObjects.AddRange(childQueryObjects);
                }
            }

            return queryObjects;
        }
        
        private bool TryAdd(TObject obj, Node<TObject> node, int depth)
        {
            if (node.bounds.Intersects(obj.Bounds) == false)
            {
                return false;
            }
            
            if (node.IsLeaf)
            {
                if (depth >= maxDepth || node.objects.Count < maxObjects)
                {
                    node.objects.Add(obj);
                }
                else
                {
                    Node<TObject>[] childrens = Split(node);

                    bool isValuesTransfered = false;
                    for (int i = 0; i < node.objects.Count; i++)
                    {
                        for (int j = 0; j < childrens.Length; j++)
                        {
                            bool isValueAdded = TryAdd(node.objects[i], childrens[j], depth + 1);

                            if (isValueAdded)
                            {
                                isValuesTransfered = true;
                                break;
                            }
                        }
                    }

                    if (isValuesTransfered)
                    {
                        node.objects.Clear();
                    }
                    
                    TryAdd(obj, node, depth);
                }
            }
            else
            {
                for (int i = 0; i < node.childrens.Length; i++)
                {
                    if (TryAdd(obj, node.childrens[i], depth + 1))
                    {
                        break;
                    }
                }
            }
            
            return true;
        }
        
        private bool TryRemove(TObject obj, Node<TObject> node)
        {
            if (node.IsLeaf)
            {
                bool isRemoved = node.objects.Remove(obj);
                return isRemoved;
            }
            
            for (int i = 0; i < node.childrens.Length; i++)
            {
                if (TryRemove(obj, node.childrens[i]))
                {
                    TryMerge(node);
                    return true;
                }
            }

            return false;
        }
        
        private Node<TObject>[] Split(Node<TObject> node)
        {
            const int quadrantsCount = 4;
            node.childrens = new Node<TObject>[quadrantsCount];

            var parentBoundary = node.bounds;
            var childSize = 0.5f * parentBoundary.size;

            Vector3 childMin = parentBoundary.center - 0.5f * childSize;
            Vector3 childMax = parentBoundary.center + 0.5f * childSize;
            
            for (int i = 0; i < quadrantsCount; i++)
            {
                var childBoundary = new Bounds(quadrantOrientationMap[i](childMin, childMax), childSize);
                node.childrens[i] = new Node<TObject>(childBoundary, maxObjects);
            }

            return node.childrens;
        }
        
        private bool TryMerge(Node<TObject> node)
        {
            Assert.IsNotNull(node.childrens, "Childrens of the node were null when merged!");
            
            int totalObjectsCount = 0;
            List<TObject> transferedObjects = new List<TObject>(capacity: maxObjects);
            
            for (int i = 0; i < node.childrens.Length; i++)
            {
                if (node.childrens[i].IsLeaf == false)
                {
                    return false;
                }
                
                totalObjectsCount += node.childrens[i].objects.Count;

                if (totalObjectsCount > maxObjects)
                {
                    return false;
                }

                transferedObjects.AddRange(node.childrens[i].objects);
            }

            node.objects.AddRange(transferedObjects);
            node.childrens = null;
            
            return true;
        }
        
        private bool TryGetSmallestNodeRelativeFor(TObject obj, Node<TObject> relativeNode, Node<TObject> parentRelativeNode, out Node<TObject> smallestNode, out Node<TObject> parentSmallestNode)
        {
            if (relativeNode.IsLeaf)
            {
                bool isObjContained = relativeNode.objects.Contains(obj);
                
                smallestNode = isObjContained ? relativeNode : null;
                parentSmallestNode = isObjContained ? parentRelativeNode : null;
                return isObjContained;
            }

            for (int i = 0; i < relativeNode.childrens.Length; i++)
            {
                if (TryGetSmallestNodeRelativeFor(
                    obj,
                    relativeNode.childrens[i], 
                    relativeNode,
                    out Node<TObject> innerSmallestNode,
                    out Node<TObject> innerParentSmallestNode
                ))
                {
                    smallestNode = innerSmallestNode;
                    parentSmallestNode = innerParentSmallestNode;
                    return true;
                }
            }

            smallestNode = null;
            parentSmallestNode = null;
            return false;
        }
    }
}
