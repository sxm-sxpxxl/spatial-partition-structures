namespace Sxm.SpatialPartitionStructures.Core.OldSeries
{
    public interface ISpatialObject<TBounds> where TBounds : struct
    {
        TBounds LocalBounds { get; }
    }
}
