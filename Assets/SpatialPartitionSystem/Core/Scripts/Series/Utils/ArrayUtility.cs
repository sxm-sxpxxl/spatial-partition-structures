namespace SpatialPartitionSystem.Core.Series
{
    internal static class ArrayUtility
    {
        public static int[] CreateArray(int capacity, int defaultValue)
        {
            int[] array = new int[capacity];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = defaultValue;
            }

            return array;
        }
    }
}
