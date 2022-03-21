using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialPartitionSystem.Core.Series
{
    internal partial class SpatialTree<TObject> : ISpatialTree<TObject> where TObject : class
    {
        public void DebugDraw(Transform relativeTransform, bool isPlaymodeOnly = false)
        {
            Assert.IsNotNull(relativeTransform);
            
            var drawer = isPlaymodeOnly ? (IDebugDrawer) new PlaymodeOnlyDebugDrawer() : new GizmosDebugDrawer();
            TraverseFromRoot(data =>
            {
                var busyColor = _nodes[data.nodeIndex].objectsCount == 0 ? Color.green : Color.red;
                drawer.SetColor(busyColor);
                        
                var worldCenter = relativeTransform.TransformPoint(_nodes[data.nodeIndex].bounds.Center);
                var worldSize = relativeTransform.TransformPoint(_nodes[data.nodeIndex].bounds.Size);

                drawer.DrawWireCube(worldCenter, worldSize);
                
                return ExecutionSignal.Continue;
            });
        }
    }
}