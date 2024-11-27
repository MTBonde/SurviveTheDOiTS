using Unity.Mathematics;

namespace SpatialHashmap
{
    public static class SpatialHashMapHelper
    {
        public static int Hash(int3 gridPos)
        {
            unchecked
            {
                return 
                    (gridPos.x * 73856093) ^ 
                    (gridPos.y * 19349663) ^ 
                    (gridPos.z * 83492791);
            }
        }

        public static int3 GetGridPosition(float3 position, float cellSize)
        {
            return new int3(math.floor(position / cellSize));
        }
    }
}