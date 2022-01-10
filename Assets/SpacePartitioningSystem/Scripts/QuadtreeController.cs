using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartitioningSystem
{
    // todo: to Quadtree separated class
    public class QuadtreeController : MonoBehaviour
    {
        [Header("Debug Info")]
        [SerializeField] private Color queryBoundaryColor = Color.green;

        [Header("Quadtree parameters")]
        [SerializeField] private Bounds boundary = new Bounds();
        [SerializeField, Range(1, 64)] private int threshold = 1;
        [SerializeField, Range(1, 128)] private int maxDepth = 16;

        [Space]
        [SerializeField] private Bounds queryBoundary = new Bounds();
        
        [Space]
        [SerializeField] private Transform[] storedObjects;
        [SerializeField] private bool isRandomObjectsAdded = false;
        [SerializeField, Range(1, 50)] private int randomObjectsCount = 10;
        
        [Space]
        [SerializeField] private Transform randomObjectPrefab;
        [SerializeField] private Transform randomObjectsContainer;

        [Space]
        [SerializeField, Range(0f, 5f)] private float delay = 2f;

        private List<Transform> quadtreeStoredObjects = new List<Transform>(capacity: 100);

        private Node<Transform> root;
        
        public class Node<T> where T : class
        {
            // todo: to props
            public Node<T>[] childrens;
            public List<T> values;
            public Bounds boundary;

            public Node(Bounds boundary, int threshold)
            {
                childrens = null;
                values = new List<T>(capacity: threshold);
                this.boundary = boundary;
            }
            
            public bool IsLeaf() => childrens == null || childrens.Length == 0;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = queryBoundaryColor;
            Gizmos.DrawCube(queryBoundary.center, queryBoundary.size);
            
            if (root == null || root.childrens == null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(boundary.center, boundary.size);
                return;
            }
            
            void DrawNestedBoundaries(Node<Transform> node)
            {
                Gizmos.color = node.values.Count == 0 
                    ? Color.green
                    : node.values.Count == threshold
                        ? Color.red
                        : Color.blue;

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

            var queryObjects = Query(queryBoundary);

            if (queryObjects != null)
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < queryObjects.Length; i++)
                {
                    Gizmos.DrawSphere(queryObjects[i].position, 0.1f);
                }
            }
        }

        private void Awake()
        {
            root = new Node<Transform>(boundary, threshold);
            HideAllObjects();
        }

        private void Start()
        {
            if (isRandomObjectsAdded)
            {
                StartCoroutine(AddRandomObjectsCoroutine());
            }
            else
            {
                StartCoroutine(AddStoredObjectsCoroutine());   
            }
        }
        
        private void HideAllObjects()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                storedObjects[i].gameObject.SetActive(false);
            }
        }

        private IEnumerator AddRandomObjectsCoroutine()
        {
            for (int i = 0; i < randomObjectsCount; i++)
            {
                float randomX = Random.Range(boundary.min.x, boundary.max.x);
                float randomY = Random.Range(boundary.min.y, boundary.max.y);

                var instance = Instantiate(randomObjectPrefab, new Vector3(randomX, randomY), Quaternion.identity, randomObjectsContainer);
                instance.name = instance.name + $" ({i})";
                instance.gameObject.SetActive(false);
                
                Debug.Log($"Adding \'{instance.name}\' random object...");
                yield return new WaitForSeconds(delay);
                
                TryAdd(instance);
                quadtreeStoredObjects.Add(instance);
                instance.gameObject.SetActive(true);
                
                Debug.Log($"Object \'{instance.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            // StartCoroutine(RemoveObjectsCoroutine());
        }
        
        private IEnumerator AddStoredObjectsCoroutine()
        {
            for (int i = 0; i < storedObjects.Length; i++)
            {
                var target = storedObjects[i];
                Debug.Log($"Adding \'{target.name}\' stored object...");
                yield return new WaitForSeconds(delay);
                
                TryAdd(target);
                quadtreeStoredObjects.Add(target);
                
                target.gameObject.SetActive(true);
                Debug.Log($"Object \'{target.name}\' was added!");
                Debug.Log($"************************************");
            }
            
            // StartCoroutine(RemoveObjectsCoroutine());
        }

        private IEnumerator RemoveObjectsCoroutine()
        {
            for (int i = 0; i < quadtreeStoredObjects.Count; i++)
            {
                var removedObject = quadtreeStoredObjects[i];
                Debug.Log($"Removing \'{removedObject.name}\' object...");
                yield return new WaitForSeconds(delay);
                
                TryRemove(removedObject);
                
                removedObject.gameObject.SetActive(false);
                Debug.Log($"Object \'{removedObject.name}\' was removed!");
                Debug.Log($"************************************");
            }

            quadtreeStoredObjects.Clear();
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

        public Transform[] Query(Bounds queryBoundary)
        {
            return Query(root, queryBoundary);
        }

        private Transform[] Query(Node<Transform> node, Bounds queryBoundary)
        {
            // todo: 100?
            List<Transform> queryObjects = new List<Transform>(capacity: 100);
            if (node.boundary.Intersects(queryBoundary))
            {
                if (node.IsLeaf())
                {
                    for (int i = 0; i < node.values.Count; i++)
                    {
                        if (queryBoundary.Contains(node.values[i].position))
                        {
                            queryObjects.Add(node.values[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < node.childrens.Length; i++)
                    {
                        Transform[] childQueryObjects = Query(node.childrens[i], queryBoundary);
                        if (childQueryObjects == null)
                        {
                            continue;
                        }
                        
                        queryObjects.AddRange(childQueryObjects);
                    }
                }
            }
            
            return queryObjects.ToArray();
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
                if (depth >= maxDepth || node.values.Count < threshold)
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
                    TryMerge(node);
                    return true;
                }
            }

            return false;
        }

        private bool TryMerge(Node<Transform> node)
        {
            // todo: asserts?
            if (node.childrens == null)
            {
                return false;
            }

            int totalObjectsCount = 0;
            List<Transform> transferedObjects = new List<Transform>(capacity: threshold);
            
            for (int i = 0; i < node.childrens.Length; i++)
            {
                if (node.childrens[i].IsLeaf() == false)
                {
                    return false;
                }
                
                totalObjectsCount += node.childrens[i].values.Count;

                if (totalObjectsCount > threshold)
                {
                    return false;
                }

                transferedObjects.AddRange(node.childrens[i].values);
            }

            node.values.AddRange(transferedObjects);
            node.childrens = null;
            
            return true;
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
                node.childrens[i] = new Node<Transform>(new Bounds(Vector3.zero, childSize), threshold); 
            }

            node.childrens[0].boundary.center = new Vector3((parentBoundary.center + childExtents).x, (parentBoundary.center + childExtents).y);
            node.childrens[1].boundary.center = new Vector3((parentBoundary.center - childExtents).x, (parentBoundary.center + childExtents).y);
            node.childrens[2].boundary.center = new Vector3((parentBoundary.center - childExtents).x, (parentBoundary.center - childExtents).y);
            node.childrens[3].boundary.center = new Vector3((parentBoundary.center + childExtents).x, (parentBoundary.center - childExtents).y);

            return node.childrens;
        }
    }
}
