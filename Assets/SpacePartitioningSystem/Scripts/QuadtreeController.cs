using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartitioningSystem
{
    // todo: to Quadtree separated class
    public class QuadtreeController : MonoBehaviour
    {
        [SerializeField] private Color boundaryColor = Color.green;
        [SerializeField] private Bounds boundary = new Bounds(); 
        [SerializeField] private Transform[] storedObjects;
        
        private const int Threshold = 1;
        private const int MaxDepth = 4;

        private Node<Transform> root;
        
        public class Node<T> where T : class
        {
            // todo: to props
            public Node<T>[] childrens;
            public List<T> values;
            public Bounds boundary;

            public Node(Bounds boundary)
            {
                childrens = null;
                values = new List<T>(capacity: Threshold);
                this.boundary = boundary;
            }
            
            public bool IsLeaf() => childrens == null || childrens.Length == 0;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = boundaryColor;

            if (root == null || root.childrens == null)
            {
                Gizmos.DrawWireCube(boundary.center, boundary.size);
                return;
            }
            
            void DrawNestedBoundaries(Node<Transform> node)
            {
                Gizmos.color = node.values.Count == 0 ? boundaryColor : Color.green;
                Gizmos.DrawWireCube(node.boundary.center, node.boundary.size);

                if (node.childrens == null)
                {
                    return;
                }

                for (int i = 0; i < node.childrens.Length; i++)
                {
                    DrawNestedBoundaries(node.childrens[i]);
                }
            }

            DrawNestedBoundaries(root);
        }

        private void Awake()
        {
            root = new Node<Transform>(boundary);
        }

        private void Start()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                TryAdd(storedObjects[i]);
            }

            StartCoroutine(RemoveFirstObjectCoroutine());
        }

        private IEnumerator RemoveFirstObjectCoroutine()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                var removedObject = storedObjects[i];
                Debug.Log($"Removing \'{removedObject.name}\' object...");
                yield return new WaitForSeconds(2f);
                
                TryRemove(removedObject);
                removedObject.gameObject.SetActive(false);
                Debug.Log($"Object \'{removedObject.name}\' was removed!");
            }
        }

        // todo: value to object rename
        public bool TryAdd(Transform value)
        {
            return TryAdd(value, root, 0);
        }

        // todo: value to object rename
        public bool TryRemove(Transform value)
        {
            return TryRemove(value, root);
        }

        private bool TryAdd(Transform value, Node<Transform> node, int depth)
        {
            if (node.boundary.Contains(value.position) == false)
            {
                return false;
            }
            
            if (node.IsLeaf())
            {
                // todo: AND max depth condition statement
                if (depth >= MaxDepth || node.values.Count < Threshold)
                {
                    node.values.Add(value);
                }
                else
                {
                    Node<Transform>[] childrens = Split(node);

                    // todo: isValuesTransfered? надо ли?
                    // todo: перенести в отдельный метод?
                    bool isValuesTransfered = false;
                    for (int i = 0; i < node.values.Count; i++)
                    {
                        for (int j = 0; j < childrens.Length; j++)
                        {
                            bool isValueAdded = TryAdd(node.values[i], childrens[j], depth + 1);

                            if (isValueAdded)
                            {
                                isValuesTransfered = true;
                                break;
                            }
                        }
                    }

                    if (isValuesTransfered)
                    {
                        node.values.Clear();
                    }
                    
                    TryAdd(value, node, depth);
                }
            }
            else
            {
                for (int i = 0; i < node.childrens.Length; i++)
                {
                    if (TryAdd(value, node.childrens[i], depth + 1))
                    {
                        break;
                    }
                }
            }
            
            return true;
        }

        // todo: node rename, value rename
        private bool TryRemove(Transform value, Node<Transform> node)
        {
            if (node.IsLeaf())
            {
                bool isRemoved = node.values.Remove(value);
                return isRemoved;
            }
            
            for (int i = 0; i < node.childrens.Length; i++)
            {
                if (TryRemove(value, node.childrens[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // todo: move to Node<T>?
        private Node<Transform>[] Split(Node<Transform> node)
        {
            const int quadrantsCount = 4;
            node.childrens = new Node<Transform>[quadrantsCount];

            var parentBoundary = node.boundary;
            var childSize = 0.5f * parentBoundary.size;
            var childExtents = 0.5f * childSize;

            for (int i = 0; i < quadrantsCount; i++)
            {
                node.childrens[i] = new Node<Transform>(new Bounds(Vector3.zero, childSize)); 
            }

            node.childrens[0].boundary.center = new Vector3((parentBoundary.center + childExtents).x, (parentBoundary.center + childExtents).y);
            node.childrens[1].boundary.center = new Vector3((parentBoundary.center - childExtents).x, (parentBoundary.center + childExtents).y);
            node.childrens[2].boundary.center = new Vector3((parentBoundary.center - childExtents).x, (parentBoundary.center - childExtents).y);
            node.childrens[3].boundary.center = new Vector3((parentBoundary.center + childExtents).x, (parentBoundary.center - childExtents).y);

            return node.childrens;
        }
    }
}
