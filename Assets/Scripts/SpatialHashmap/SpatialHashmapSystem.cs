using System;
using ECS.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SpatialHashmap
{
    [BurstCompile]
    public partial struct SpatialHashmapSystem : ISystem
    {
        private NativeParallelMultiHashMap<int3, Entity> spatialHashMap;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
            
            //float cellSize = SystemAPI.GetSingleton<BoidSettings>().NeighborRadius;
            int estimatedCapacity = 1024 * 11; 
            spatialHashMap = new NativeParallelMultiHashMap<int3, Entity>(estimatedCapacity, Allocator.Persistent);
        }
        
        public void OnDestroy(ref SystemState state)
        {
            spatialHashMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float cellSize = SystemAPI.GetSingleton<BoidSettings>().NeighborRadius;

            // Create the hashmap
            //NativeParallelMultiHashMap<int3, Entity> spatialHashMap = new NativeParallelMultiHashMap<int3, Entity>(1024, Allocator.TempJob);

            // Collect boids into the hashmap
            CollectEntitiesToHashmapJob collectJob = new CollectEntitiesToHashmapJob
            {
                CellSize = cellSize,
                SpatialHashMap = spatialHashMap.AsParallelWriter()
            };
            state.Dependency = collectJob.ScheduleParallel(state.Dependency);


            // Use the hashmap for neighbor queries
            QueryNeighborsWithHashmapJob queryNeighborsWithHashmapJob = new QueryNeighborsWithHashmapJob
            {
                CellSize = cellSize,
                SpatialHashMap = spatialHashMap,
                BoidSettings = SystemAPI.GetSingleton<BoidSettings>()
            };
            state.Dependency = queryNeighborsWithHashmapJob.ScheduleParallel(state.Dependency);

            
            spatialHashMap.Clear();
        }

        [BurstCompile]
        private partial struct CollectEntitiesToHashmapJob : IJobEntity
        {
            public float CellSize;
            [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int3, Entity>.ParallelWriter SpatialHashMap;
            
            public void Execute(Entity entity, in LocalTransform transform)
            {
                float3 position = transform.Position;
                int3 cellIndex = new int3(
                    (int)math.floor(position.x / CellSize),
                    (int)math.floor(position.y / CellSize),
                    (int)math.floor(position.z / CellSize)
                );

                SpatialHashMap.Add(cellIndex, entity);
            }
        }

        [BurstCompile]
        private partial struct QueryNeighborsWithHashmapJob : IJobEntity
        {
            public float CellSize;
            [ReadOnly] public NativeParallelMultiHashMap<int3, Entity> SpatialHashMap;
            [ReadOnly] public NativeArray<HashAndIndex> HashAndIndices;
            [ReadOnly] public BoidSettings BoidSettings;

            public void Execute(Entity entity, in LocalTransform transform)
            {
                float3 position = transform.Position;
                int3 cellIndex = new int3(
                    (int)math.floor(position.x / CellSize),
                    (int)math.floor(position.y / CellSize),
                    (int)math.floor(position.z / CellSize)
                );

                NativeList<Entity> neighbors = new NativeList<Entity>(Allocator.Temp);

                // Check the current cell and adjacent cells
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            int hash = SpatialHashMapHelper.Hash(new int3(x, y, z));
                            int startIndex = BinarySearchFirst(HashAndIndices, hash);
                            
                            
                            int3 neighborCell = cellIndex + new int3(x, y, z);
                            if (SpatialHashMap.TryGetFirstValue(neighborCell, out Entity neighbor, out var iterator))
                            {
                                do
                                {
                                    if (neighbor != entity)
                                    {
                                        float distance = math.distance(position, transform.Position);
                                        if (distance < BoidSettings.NeighborRadius)
                                        {
                                            neighbors.Add(neighbor);
                                        }
                                    }
                                } while (SpatialHashMap.TryGetNextValue(out neighbor, ref iterator));
                            }
                        }
                    }
                }

                // Use neighbors for behavior calculations
                neighbors.Dispose();
            }
    
            private int BinarySearchFirst(NativeArray<HashAndIndex> array, int hash)
            {
                int left = 0, right = array.Length - 1;
                while (left <= right)
                {
                    int mid = (left + right) / 2;
                    if (array[mid].Hash == hash) return mid;
                    if (array[mid].Hash < hash) left = mid + 1;
                    else right = mid - 1;
                }
             
                return -1;
            }
        }
    }
    
    public static class SpatialHashMapHelper
    {
        public static int3 GetGridPosition(float3 position, float cellSize)
        {
            return new int3
            (
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize),
                (int)math.floor(position.z / cellSize)
            );
        }
    
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
    }
    
    public struct HashAndIndex : IComparable<HashAndIndex>
    {
        public int Hash;  
        public int Index; 

        public int CompareTo(HashAndIndex other) => Hash.CompareTo(other.Hash);
    }
}