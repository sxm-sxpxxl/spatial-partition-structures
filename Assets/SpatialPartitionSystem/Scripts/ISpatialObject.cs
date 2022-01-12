using UnityEngine;

namespace SpatialPartitionSystem
{
    public interface ISpatialObject
    {
        Bounds Bounds { get; }
    }
}
