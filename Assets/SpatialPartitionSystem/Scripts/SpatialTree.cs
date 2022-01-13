using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem
{
    public abstract class SpatialTree<TObject> where TObject : class, ISpatialObject
    {
        protected abstract Dictionary<int, Func<Vector3, Vector3, Vector3>> QuadrantOrientationMap { get; }

        private readonly int _maxObjects = 0, _maxDepth = 0;
        private readonly Node<TObject> _root;

        /// <summary>
        /// Constructs a new tree with given settings.
        /// </summary>
        /// <param name="bounds">The bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        protected SpatialTree(Bounds bounds, int maxObjects, int maxDepth)
        {
            _root = new Node<TObject>(bounds, maxObjects);
            _maxObjects = maxObjects;
            _maxDepth = maxDepth;
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
                var busyColor = node.Objects.Count == 0
                    ? Color.green
                    : node.Objects.Count >= _maxObjects
                        ? Color.red
                        : Color.blue;

                drawer.SetColor(busyColor);
                drawer.DrawWireCube(node.Bounds.center, node.Bounds.size);

                if (node.Childrens == null)
                {
                    return;
                }

                foreach (var child in node.Childrens)
                {
                    DrawNestedBounds(child);
                }
            }

            DrawNestedBounds(_root);
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
            return TryAdd(obj, _root, 0);
        }
        
        /// <summary>
        /// Tries to remove the object from the tree.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true - if the object is removed; false - if not.</returns>
        public bool TryRemove(TObject obj)
        {
            Assert.IsNotNull(obj);
            return TryRemove(obj, _root);
        }
        
        /// <summary>
        /// Clears the whole tree.
        /// </summary>
        public void Clear()
        {
            _root.Childrens = null;
            _root.Objects.Clear();
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
                _root,
                null,
                out Node<TObject> smallestNode,
                out Node<TObject> parentSmallestNode
            ) == false)
            {
                TryAdd(obj, _root, 0);
                return;
            }
            
            if (smallestNode.Bounds.Intersects(obj.Bounds) == false)
            {
                TryRemove(obj, smallestNode);
                TryAdd(obj, _root, 0);
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
            return TryGetSmallestNodeRelativeFor(obj, _root, null, out _, out _);
        }

        /// <summary>
        /// Whether the bounds are completely inside the tree or not.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool Contains(Bounds bounds)
        {
            return _root.Bounds.Contains(bounds.min) && _root.Bounds.Contains(bounds.max);
        }
        
        /// <summary>
        /// Returns an enumerable which iterates over all objects overlapping the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to check.</param>
        /// <param name="maxQueryObjects">The maximum number of query objects for more efficient memory allocation for the collection.</param>
        /// <returns></returns>
        public IEnumerable<TObject> Query(Bounds bounds, int maxQueryObjects = 1000)
        {
            return Query(_root, bounds, maxQueryObjects);
        }

        private IEnumerable<TObject> Query(Node<TObject> node, Bounds queryBounds, int maxQueryObjects)
        {
            if (node.Bounds.Intersects(queryBounds) == false)
            {
                return null;
            }
            
            var queryObjects = new List<TObject>(capacity: maxQueryObjects);
            
            if (node.IsLeaf)
            {
                for (int i = 0; i < node.Objects.Count; i++)
                {
                    if (queryBounds.Intersects(node.Objects[i].Bounds))
                    {
                        queryObjects.Add(node.Objects[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < node.Childrens.Length; i++)
                {
                    var childQueryObjects = Query(node.Childrens[i], queryBounds, (int) 0.5f * maxQueryObjects);
                    
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
            if (node.Bounds.Intersects(obj.Bounds) == false)
            {
                return false;
            }
            
            if (node.IsLeaf)
            {
                if (depth >= _maxDepth || node.Objects.Count < _maxObjects)
                {
                    node.Objects.Add(obj);
                }
                else
                {
                    Node<TObject>[] childrens = Split(node);

                    bool isValuesTransfered = false;
                    for (int i = 0; i < node.Objects.Count; i++)
                    {
                        for (int j = 0; j < childrens.Length; j++)
                        {
                            bool isValueAdded = TryAdd(node.Objects[i], childrens[j], depth + 1);

                            if (isValueAdded)
                            {
                                isValuesTransfered = true;
                                break;
                            }
                        }
                    }

                    if (isValuesTransfered)
                    {
                        node.Objects.Clear();
                    }
                    
                    TryAdd(obj, node, depth);
                }
            }
            else
            {
                for (int i = 0; i < node.Childrens.Length; i++)
                {
                    if (TryAdd(obj, node.Childrens[i], depth + 1))
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
                bool isRemoved = node.Objects.Remove(obj);
                return isRemoved;
            }
            
            for (int i = 0; i < node.Childrens.Length; i++)
            {
                if (TryRemove(obj, node.Childrens[i]))
                {
                    TryMerge(node);
                    return true;
                }
            }

            return false;
        }
        
        private Node<TObject>[] Split(Node<TObject> node)
        {
            node.Childrens = new Node<TObject>[QuadrantOrientationMap.Count];

            var parentBoundary = node.Bounds;
            var childSize = 0.5f * parentBoundary.size;

            Vector3 childMin = parentBoundary.center - 0.5f * childSize;
            Vector3 childMax = parentBoundary.center + 0.5f * childSize;
            
            for (int i = 0; i < QuadrantOrientationMap.Count; i++)
            {
                var childBoundary = new Bounds(QuadrantOrientationMap[i](childMin, childMax), childSize);
                node.Childrens[i] = new Node<TObject>(childBoundary, _maxObjects);
            }

            return node.Childrens;
        }
        
        private bool TryMerge(Node<TObject> node)
        {
            Assert.IsNotNull(node.Childrens, "Childrens of the node were null when merged!");
            
            int totalObjectsCount = 0;
            List<TObject> transferedObjects = new List<TObject>(capacity: _maxObjects);
            
            for (int i = 0; i < node.Childrens.Length; i++)
            {
                if (node.Childrens[i].IsLeaf == false)
                {
                    return false;
                }
                
                totalObjectsCount += node.Childrens[i].Objects.Count;

                if (totalObjectsCount > _maxObjects)
                {
                    return false;
                }

                transferedObjects.AddRange(node.Childrens[i].Objects);
            }

            node.Objects.AddRange(transferedObjects);
            node.Childrens = null;
            
            return true;
        }
        
        private bool TryGetSmallestNodeRelativeFor(TObject obj, Node<TObject> relativeNode, Node<TObject> parentRelativeNode, out Node<TObject> smallestNode, out Node<TObject> parentSmallestNode)
        {
            if (relativeNode.IsLeaf)
            {
                bool isObjContained = relativeNode.Objects.Contains(obj);
                
                smallestNode = isObjContained ? relativeNode : null;
                parentSmallestNode = isObjContained ? parentRelativeNode : null;
                return isObjContained;
            }

            for (int i = 0; i < relativeNode.Childrens.Length; i++)
            {
                if (TryGetSmallestNodeRelativeFor(
                    obj,
                    relativeNode.Childrens[i], 
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