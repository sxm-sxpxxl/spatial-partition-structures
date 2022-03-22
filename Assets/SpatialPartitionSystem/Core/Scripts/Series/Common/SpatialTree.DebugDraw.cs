using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal sealed partial class SpatialTree<TObject, TBounds, TVector> : ISpatialTree<TObject, TBounds, TVector>
        where TObject : class
        where TBounds : IAABB<TVector>
        where TVector : struct
    {
        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();
            TraverseFromRoot(data =>
            {
                var busyColor = _nodes[data.nodeIndex].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                Vector3 worldCenter = _nodes[data.nodeIndex].bounds.TransformCenter(relativeTransform);
                Vector3 worldSize = _nodes[data.nodeIndex].bounds.TransformSize(relativeTransform);

                drawer.DrawWireCube(worldCenter, worldSize);
                
                return ExecutionSignal.Continue;
            });
        }
    }
}