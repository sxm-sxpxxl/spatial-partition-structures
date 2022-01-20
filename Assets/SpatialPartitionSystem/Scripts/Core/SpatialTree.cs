using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core
{
    public abstract class SpatialTree<TObject, TBounds>
        where TObject : class, ISpatialObject<TBounds>
        where TBounds : struct
    {
        public int Count => Query(_root, _root.Bounds, 100).ToArray().Length;
        public int MaxObjects => _maxObjects;
        public int MaxDepth => _maxDepth;

        protected abstract Dictionary<int, Func<Vector3, Vector3, Vector3>> QuadrantOrientationMap { get; }
        
        private const int MAX_POSSIBLE_DEPTH = 16, MAX_POSSIBLE_OBJECTS = 8;

        private readonly int _maxObjects = 0, _maxDepth = 0;
        internal readonly Node<TObject, TBounds> _root;
        private readonly List<TObject> _missingObjects = new List<TObject>(capacity: 100);

        /// <summary>
        /// Constructs a new tree with given settings.
        /// </summary>
        /// <param name="bounds">The bounds of the tree.</param>
        /// <param name="maxObjects">The maximum number of objects per node.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        protected SpatialTree(TBounds bounds, int maxObjects, int maxDepth)
        {
            _root = new Node<TObject, TBounds>(bounds, maxObjects);
            _maxObjects = Mathf.Clamp(maxObjects, 0, MAX_POSSIBLE_OBJECTS);
            _maxDepth = Mathf.Clamp(maxDepth, 0, MAX_POSSIBLE_DEPTH);
        }

        /// <summary>
        /// Whether the bounds are completely inside the tree or not.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public abstract bool Contains(TBounds bounds);
        
        protected abstract bool Intersects(TBounds a, TBounds b);

        protected abstract Vector3 GetBoundsCenter(TBounds bounds);
        
        protected abstract Vector3 GetBoundsSize(TBounds bounds);

        protected abstract TBounds CreateBounds(Vector3 center, Vector3 size);

        /// <summary>
        /// Draws the bounds of the tree using Debug.DrawLine if playmode only setting is true or Gizmos.DrawLine if not.
        /// </summary>
        /// <param name="isPlaymodeOnly">The playmode only setting.</param>
        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();

            void DrawNestedBounds(Node<TObject, TBounds> node)
            {
                var busyColor = node.Objects.Count == 0
                    ? Color.green
                    : node.Objects.Count >= _maxObjects
                        ? Color.red
                        : Color.blue;

                drawer.SetColor(busyColor);

                var worldCenter = relativeTransform.TransformPoint(GetBoundsCenter(node.Bounds));
                var worldSize = relativeTransform.TransformDirection(GetBoundsSize(node.Bounds));
                
                drawer.DrawWireCube(worldCenter, worldSize);

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
                out Node<TObject, TBounds> smallestNode,
                out Node<TObject, TBounds> parentSmallestNode
            ) == false)
            {
                TObject missiongObject = _missingObjects.Find(x => x == obj);

                if (missiongObject != null && TryAdd(missiongObject))
                {
                    _missingObjects.Remove(missiongObject);
                }
                
                return;
            }
            
            if (Intersects(smallestNode.Bounds, obj.LocalBounds) == false)
            {
                bool isRemoved = TryRemove(obj, smallestNode);
                bool isAdded = TryAdd(obj, _root, 0);

                if (isRemoved && isAdded == false)
                {
                    _missingObjects.Add(obj);
                }
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
        /// Get all read only nodes of the tree.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IReadOnlyNode<TObject, TBounds>> GetAllNodes()
        {
            return GetSubtreeFor(_root);
        }

        /// <summary>
        /// Get read only nodes of the tree for a given depth.
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IReadOnlyList<IReadOnlyNode<TObject, TBounds>> GetNodesFor(int depth)
        {
            depth = Mathf.Clamp(depth, 0, _maxDepth);
            return GetChildrensForDepth(_root, 0, depth);
        }

        /// <summary>
        /// Returns an enumerable which iterates over all objects overlapping the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to check.</param>
        /// <param name="maxQueryObjects">The maximum number of query objects for more efficient memory allocation for the collection.</param>
        /// <returns></returns>
        public IEnumerable<TObject> Query(TBounds bounds, int maxQueryObjects = 1000)
        {
            var queryObjects = Query(_root, bounds, maxQueryObjects);
            return queryObjects == null ? new TObject[0] : queryObjects;
        }

        private bool TryAdd(TObject obj, Node<TObject, TBounds> node, int depth)
        {
            if (Intersects(node.Bounds, obj.LocalBounds) == false)
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
                    Node<TObject, TBounds>[] childrens = Split(node);

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
        
        private bool TryRemove(TObject obj, Node<TObject, TBounds> node)
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
        
        private Node<TObject, TBounds>[] Split(Node<TObject, TBounds> node)
        {
            node.Childrens = new Node<TObject, TBounds>[QuadrantOrientationMap.Count];

            var parentBoundary = node.Bounds;
            var childSize = 0.5f * GetBoundsSize(parentBoundary);

            Vector3 childMin = GetBoundsCenter(parentBoundary) - 0.5f * childSize;
            Vector3 childMax = GetBoundsCenter(parentBoundary) + 0.5f * childSize;
            
            for (int i = 0; i < QuadrantOrientationMap.Count; i++)
            {
                var childBoundary = CreateBounds(QuadrantOrientationMap[i](childMin, childMax), childSize);
                node.Childrens[i] = new Node<TObject, TBounds>(childBoundary, _maxObjects);
            }

            return node.Childrens;
        }
        
        private bool TryMerge(Node<TObject, TBounds> node)
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
        
        private IEnumerable<TObject> Query(Node<TObject, TBounds> node, TBounds queryBounds, int maxQueryObjects)
        {
            if (Intersects(node.Bounds, queryBounds) == false)
            {
                return null;
            }
            
            var queryObjects = new List<TObject>(capacity: maxQueryObjects);
            
            if (node.IsLeaf)
            {
                for (int i = 0; i < node.Objects.Count; i++)
                {
                    if (Intersects(node.Objects[i].LocalBounds, queryBounds))
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
        
        private bool TryGetSmallestNodeRelativeFor(
            TObject obj,
            Node<TObject, TBounds> relativeNode,
            Node<TObject, TBounds> parentRelativeNode,
            out Node<TObject, TBounds> smallestNode,
            out Node<TObject, TBounds> parentSmallestNode
        )
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
                    out Node<TObject, TBounds> innerSmallestNode,
                    out Node<TObject, TBounds> innerParentSmallestNode
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
        
        private List<Node<TObject, TBounds>> GetChildrensForDepth(Node<TObject, TBounds> node, int actualDepth, int requiredDepth)
        {
            if (node.IsLeaf == false)
            {
                int parentRequiredDepth = requiredDepth - 1;
                if (actualDepth == parentRequiredDepth)
                {
                    return node.Childrens.ToList();
                }

                for (int i = 0; i < node.Childrens.Length; i++)
                {
                    var childrens = GetChildrensForDepth(node.Childrens[i], actualDepth + 1, requiredDepth);

                    if (childrens != null)
                    {
                        return childrens;
                    }
                }
            }

            return null;
        }

        private List<Node<TObject, TBounds>> GetSubtreeFor(Node<TObject, TBounds> node)
        {
            Assert.IsNotNull(node);
            var subtree = new HashSet<Node<TObject, TBounds>> { node };

            if (node.IsLeaf == false)
            {
                for (int i = 0; i < node.Childrens.Length; i++)
                {
                    subtree.Add(node.Childrens[i]);
                    var childSubtree = GetSubtreeFor(node.Childrens[i]);

                    for (int j = 0; j < childSubtree.Count; j++)
                    {
                        subtree.Add(childSubtree[j]);
                    }
                }
            }
            
            return subtree.ToList();
        }
    }
}
