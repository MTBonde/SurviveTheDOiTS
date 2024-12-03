// using ECS.Authoring;
// using ECS.Components;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace ECS.Systems
// {
//     /// <summary>
//     /// Position-based collision system using the spatial hashmap from BoidBehaviorSystem.
//     /// </summary>
//     [BurstCompile]
//     public partial struct SpatialCollisionSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<BoidSettings>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();
//
//             // Query for bullet data
//             var bulletQuery = SystemAPI.QueryBuilder()
//                 .WithAll<BulletComponent, LocalTransform>()
//                 .Build();
//             int bulletCount = bulletQuery.CalculateEntityCount();
//             NativeArray<Entity> bulletEntities = bulletQuery.ToEntityArray(Allocator.TempJob);
//             NativeArray<float3> bulletPositions = new NativeArray<float3>(bulletCount, Allocator.TempJob);
//
//             // Collect bullet positions
//             var collectBulletDataJob = new CollectBulletDataJob
//             {
//                 BulletPositions = bulletPositions
//             };
//             state.Dependency = collectBulletDataJob.ScheduleParallel(bulletQuery, state.Dependency);
//             state.Dependency.Complete();
//
//             // Query for boid data
//             var boidQuery = SystemAPI.QueryBuilder()
//                 .WithAll<BoidTag, LocalTransform>()
//                 .Build();
//             int boidCount = boidQuery.CalculateEntityCount();
//             NativeArray<Entity> boidEntities = boidQuery.ToEntityArray(Allocator.TempJob);
//             NativeArray<float3> boidPositions = new NativeArray<float3>(boidCount, Allocator.TempJob);
//
//             // Collect boid positions
//             var collectBoidDataJob = new CollectBoidDataJob
//             {
//                 BoidPositions = boidPositions
//             };
//             state.Dependency = collectBoidDataJob.ScheduleParallel(boidQuery, state.Dependency);
//             state.Dependency.Complete();
//
//             // Reuse spatial hashmap
//             float cellSize = boidSettings.SpatialCellSize;
//             NativeParallelMultiHashMap<int, int> spatialHashMap = new NativeParallelMultiHashMap<int, int>(boidCount, Allocator.TempJob);
//
//             var buildHashMapJob = new BuildSpatialHashMapJob
//             {
//                 Positions = boidPositions,
//                 SpatialHashMap = spatialHashMap.AsParallelWriter(),
//                 CellSize = cellSize
//             };
//             state.Dependency = buildHashMapJob.Schedule(boidCount, 64, state.Dependency);
//             state.Dependency.Complete();
//
//             // Perform collision detection
//             var collisionDetectionJob = new CollisionDetectionJob
//             {
//                 BulletEntities = bulletEntities,
//                 BulletPositions = bulletPositions,
//                 BoidEntities = boidEntities,
//                 BoidPositions = boidPositions,
//                 SpatialHashMap = spatialHashMap,
//                 CellSize = cellSize,
//                 CommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//                     .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
//             };
//             state.Dependency = collisionDetectionJob.Schedule(bulletCount, 64, state.Dependency);
//             state.Dependency.Complete();
//
//             // Dispose native arrays
//             bulletEntities.Dispose(state.Dependency);
//             bulletPositions.Dispose(state.Dependency);
//             boidEntities.Dispose(state.Dependency);
//             boidPositions.Dispose(state.Dependency);
//             spatialHashMap.Dispose(state.Dependency);
//         }
//
//         [BurstCompile]
//         public partial struct CollectBulletDataJob : IJobEntity
//         {
//             public NativeArray<float3> BulletPositions;
//
//             public void Execute([EntityIndexInQuery] int index, in LocalTransform transform)
//             {
//                 BulletPositions[index] = transform.Position;
//             }
//         }
//
//         [BurstCompile]
//         public partial struct CollectBoidDataJob : IJobEntity
//         {
//             public NativeArray<float3> BoidPositions;
//
//             public void Execute([EntityIndexInQuery] int index, in LocalTransform transform)
//             {
//                 BoidPositions[index] = transform.Position;
//             }
//         }
//
//         [BurstCompile]
//         public struct BuildSpatialHashMapJob : IJobParallelFor
//         {
//             [ReadOnly] public NativeArray<float3> Positions;
//             public NativeParallelMultiHashMap<int, int>.ParallelWriter SpatialHashMap;
//             public float CellSize;
//
//             public void Execute(int index)
//             {
//                 float3 position = Positions[index];
//                 int3 cell = GridPosition(position, CellSize);
//                 int hash = Hash(cell);
//
//                 SpatialHashMap.Add(hash, index);
//             }
//
//             public static int3 GridPosition(float3 position, float cellSize) =>
//                 new int3(math.floor(position / cellSize));
//
//             public static int Hash(int3 gridPos) =>
//                 (gridPos.x * 73856093) ^ (gridPos.y * 19349669) ^ (gridPos.z * 83492791);
//         }
//
//         [BurstCompile]
//         public struct CollisionDetectionJob : IJobParallelFor
//         {
//             [ReadOnly] public NativeArray<Entity> BulletEntities;
//             [ReadOnly] public NativeArray<float3> BulletPositions;
//             [ReadOnly] public NativeArray<Entity> BoidEntities;
//             [ReadOnly] public NativeArray<float3> BoidPositions;
//             [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialHashMap;
//             public float CellSize;
//             public EntityCommandBuffer.ParallelWriter CommandBuffer;
//
//             public void Execute(int bulletIndex)
//             {
//                 float3 bulletPosition = BulletPositions[bulletIndex];
//                 int3 bulletCell = BuildSpatialHashMapJob.GridPosition(bulletPosition, CellSize);
//                 int hash = BuildSpatialHashMapJob.Hash(bulletCell);
//
//                 if (SpatialHashMap.TryGetFirstValue(hash, out int boidIndex, out var iterator))
//                 {
//                     do
//                     {
//                         float3 boidPosition = BoidPositions[boidIndex];
//                         if (math.distance(bulletPosition, boidPosition) <= 0.5f) // Example collision radius
//                         {
//                             CommandBuffer.DestroyEntity(bulletIndex, BulletEntities[bulletIndex]);
//                             CommandBuffer.DestroyEntity(boidIndex, BoidEntities[boidIndex]);
//                             break; // Stop checking further once a collision is detected
//                         }
//                     } while (SpatialHashMap.TryGetNextValue(out boidIndex, ref iterator));
//                 }
//             }
//         }
//     }
// }
