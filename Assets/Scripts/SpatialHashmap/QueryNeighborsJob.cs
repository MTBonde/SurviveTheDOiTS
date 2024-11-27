using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SpatialHashmap
{
    [BurstCompile]
    public struct QueryNeighborsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<SpatialEntity> Entities;
        [ReadOnly] public NativeArray<HashAndIndex> HashAndIndices;
        [ReadOnly] public float CellSize;
        [ReadOnly] public float QueryRadius;

        public NativeParallelMultiHashMap<int, int>.ParallelWriter NeighborMap;

        public void Execute(int index)
        {
            SpatialEntity currentEntity = Entities[index];
            float3 queryPosition = currentEntity.Position;
            float radiusSquared = QueryRadius * QueryRadius;

            int3 minGridPos = SpatialHashMapHelper.GetGridPosition(queryPosition - QueryRadius, CellSize);
            int3 maxGridPos = SpatialHashMapHelper.GetGridPosition(queryPosition + QueryRadius, CellSize);

            for (int x = minGridPos.x; x <= maxGridPos.x; x++)
            {
                for (int y = minGridPos.y; y <= maxGridPos.y; y++)
                {
                    for (int z = minGridPos.z; z <= maxGridPos.z; z++)
                    {
                        int hash = SpatialHashMapHelper.Hash(new int3(x, y, z));
                        int startIndex = BinarySearchFirst(HashAndIndices, hash);

                        if (startIndex < 0) continue;

                        for (int i = startIndex; i < HashAndIndices.Length && HashAndIndices[i].Hash == hash; i++)
                        {
                            int neighborIndex = HashAndIndices[i].Index;

                            // Skip self
                            if (neighborIndex == index) continue;

                            float3 neighborPosition = Entities[neighborIndex].Position;

                            if (math.distancesq(neighborPosition, queryPosition) <= radiusSquared)
                            {
                                // Add neighbor to the NeighborMap
                                NeighborMap.Add(index, neighborIndex);
                            }
                        }
                    }
                }
            }
        }

        private int BinarySearchFirst(NativeArray<HashAndIndex> array, int hash)
        {
            int left = 0, right = array.Length - 1;
            int result = -1;
            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (array[mid].Hash == hash)
                {
                    result = mid;
                    right = mid - 1; // Keep searching to the left for the first occurrence
                }
                else if (array[mid].Hash < hash)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }
    }
}
