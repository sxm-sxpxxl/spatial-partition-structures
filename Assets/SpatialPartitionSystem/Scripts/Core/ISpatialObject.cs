using UnityEngine;

namespace SpatialPartitionSystem.Core
{
    public interface ISpatialObject
    {
        Bounds Bounds { get; }
    }
}
