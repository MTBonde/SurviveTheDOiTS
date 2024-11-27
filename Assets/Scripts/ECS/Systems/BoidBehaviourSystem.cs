using ECS.Authoring;
using ECS.Components;
using SpatialHashmap;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public struct BoidData
    {
        public float3 Position;
        public float3 Velocity;
    }

    /// <summary>
    /// A crude test implementation of a boid behavior system.
    /// No optimizations have been made, this is just a simple test.
    /// First retrieve the boid settings from the singleton, then query for all boids in the scene, with specefic components.
    /// Allocate memory for nativearrays to store boid data and entities.
    /// then create and schuule data collections and behavior jobs.
    /// Last dispose of native arrays
    /// </summary>
    [BurstCompile]
    public partial struct BoidBehaviorSystem : ISystem
    {
        // private NativeArray<SpatialEntity> spatialEntities;
        // private NativeArray<HashAndIndex> hashAndIndices;
        // private NativeList<int> neighborIndices;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Retrieve BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();

            // Query for all boids in the scene, with BoidAttackComponent disabled
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<BoidTag, LocalTransform, VelocityComponent>()
                .WithDisabled<BoidAttackComponent>()
                .Build();

            // Get boid count
            int boidCount = boidQuery.CalculateEntityCount();

            // Early exit if no boids
            if (boidCount == 0)
            {
                return;
            }
            
            // Allocate the arrays
            var entities = boidQuery.ToEntityArray(Allocator.TempJob);
            var positions = boidQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            
            int maxNeighborsPerBoid = (int)boidSettings.NeighborRadius * 10;
            int maxBoidCount = 10000;
            int initialCapacity = maxBoidCount * maxNeighborsPerBoid;

            // Allocate arrays for spatial data
            using (var spatialEntities = new NativeArray<SpatialEntity>(boidCount, Allocator.TempJob))
            using (var hashAndIndices = new NativeArray<HashAndIndex>(boidCount, Allocator.TempJob))
            using (var neighborMap = new NativeParallelMultiHashMap<int, int>(initialCapacity, Allocator.TempJob))
            {
                // Create the job
                var collectSpatialEntitiesJob = new CollectSpatialEntitiesJob
                {
                    SpatialEntities = spatialEntities,
                    Entities = entities,
                    Positions = positions
                };

                // Schedule and complete the job
                JobHandle collectBoidsJobHandle = collectSpatialEntitiesJob.Schedule(boidCount, 64);
                collectBoidsJobHandle.Complete();

                // Dispose of the arrays
                entities.Dispose();
                positions.Dispose();

                // Hash spatial entities
                var hashBoidsJob = new HashEntitiesJob
                {
                    Entities = spatialEntities,
                    HashAndIndices = hashAndIndices,
                    CellSize = boidSettings.NeighborRadius
                };
                JobHandle hashBoidsJobHandle = hashBoidsJob.Schedule(spatialEntities.Length, 64, collectBoidsJobHandle);

                // Sort hashed data
                var sortHashedBoidsJob = new SortHashCodesJob
                {
                    HashAndIndices = hashAndIndices
                };
                JobHandle sortJobHandle = sortHashedBoidsJob.Schedule(hashBoidsJobHandle);

                // Query neighbors using spatial hashmap
                var queryNeighboringBoidsJob = new QueryNeighborsJob
                {
                    Entities = spatialEntities,
                    HashAndIndices = hashAndIndices,
                    NeighborMap = neighborMap.AsParallelWriter(),
                    QueryRadius = boidSettings.NeighborRadius,
                    CellSize = boidSettings.NeighborRadius
                };
                JobHandle queryNeighboringBoidsJobHandle = queryNeighboringBoidsJob.Schedule(spatialEntities.Length, 64, sortJobHandle);
                queryNeighboringBoidsJobHandle.Complete();

                // Perform boid behavior using neighbor data
                var boidBehaviorJob = new BoidBehaviorJob
                {
                    DeltaTime = deltaTime,
                    BoidSettings = boidSettings,
                    SpatialEntities = spatialEntities,
                    NeighborMap = neighborMap,
                    LocalTransformLookup = state.GetComponentLookup<LocalTransform>(false),
                    VelocityLookup = state.GetComponentLookup<VelocityComponent>(false),
                };
                JobHandle boidBehaviourJobHandle = boidBehaviorJob.Schedule(boidCount, 64);
                boidBehaviourJobHandle.Complete();
            }
        }

        /// <summary>
        /// A job that collects boid data such as position and velocity for all boids in the scene.
        /// This job processes entities with the LocalTransform and VelocityComponent components,
        /// storing their data into a NativeArray of BoidData.
        /// </summary>
        [BurstCompile]
        public partial struct CollectBoidDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<BoidData> BoidDataArray;

            public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in VelocityComponent velocity)
            {
                BoidDataArray[index] = new BoidData
                {
                    Position = transform.Position,
                    Velocity = velocity.Velocity
                };
            }
        }

        /// <summary>
        /// A job for handling individual boid behavior updates in parallel.
        /// It calculates the movement for each boid based on its velocity and the provided settings.
        /// The job works in parallel for each boid in the BoidDataArray.
        /// </summary>
        [BurstCompile]
        public struct BoidBehaviorJob : IJobParallelFor
        {
            public float DeltaTime;
            public BoidSettings BoidSettings;

            [ReadOnly] public NativeArray<SpatialEntity> SpatialEntities;
            [ReadOnly] public NativeParallelMultiHashMap<int, int> NeighborMap;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            
            [NativeDisableParallelForRestriction]
            public ComponentLookup<VelocityComponent> VelocityLookup;

            public void Execute(int index)
            {
                SpatialEntity currentBoid = SpatialEntities[index];
                float3 currentPosition = currentBoid.Position;
                float3 currentVelocity = VelocityLookup[currentBoid.Entity].Velocity;

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;
                int neighborCount = 0;

                // Get the neighbors for the current boid
                NativeParallelMultiHashMapIterator<int> iterator;
                int neighborIndex;
                if (NeighborMap.TryGetFirstValue(index, out neighborIndex, out iterator))
                {
                    do
                    {
                        if (neighborIndex == index) continue;

                        SpatialEntity neighborBoid = SpatialEntities[neighborIndex];
                        float3 neighborPosition = neighborBoid.Position;
                        float3 neighborVelocity = VelocityLookup[neighborBoid.Entity].Velocity;

                        float distance = math.distance(currentPosition, neighborPosition);

                        if (distance > 0 && distance < BoidSettings.NeighborRadius)
                        {
                            alignment += neighborVelocity;
                            cohesion += neighborPosition;
                            separation += (currentPosition - neighborPosition) / distance;
                            neighborCount++;
                        }

                    } while (NeighborMap.TryGetNextValue(out neighborIndex, ref iterator));
                }

                // Calculate average alignment, cohesion, and separation
                if (neighborCount > 0)
                {
                    alignment = alignment / neighborCount;
                    alignment = math.normalize(alignment) * BoidSettings.AlignmentWeight;

                    cohesion = (cohesion / neighborCount) - currentPosition;
                    cohesion = math.normalize(cohesion) * BoidSettings.CohesionWeight;

                    separation = separation / neighborCount;
                    separation = math.normalize(separation) * BoidSettings.SeparationWeight;
                }

                // Calculate acceleration
                float3 acceleration = alignment + cohesion + separation;

                // Update velocity
                currentVelocity += acceleration * DeltaTime;

                // Limit speed
                float speed = math.length(currentVelocity);
                if (speed > BoidSettings.MoveSpeed)
                {
                    currentVelocity = (currentVelocity / speed) * BoidSettings.MoveSpeed;
                }

                // Update position
                currentPosition += currentVelocity * DeltaTime;
                
                // naive sphere boundary checking
                // Spherical boundary checking
                float distanceFromCenter = math.distance(currentPosition, BoidSettings.BoundaryCenter);
                if (distanceFromCenter > BoidSettings.BoundarySize)
                {
                    // Calculate steer direction towards the center
                    float3 directionToCenter = math.normalize(BoidSettings.BoundaryCenter - currentPosition);
                    float3 steer = directionToCenter * BoidSettings.BoundaryWeight;

                    // Apply steering to bring the boid back inside the sphere
                    currentVelocity += steer * DeltaTime;
                }


                // // Boundary checking
                // // (naive approach, not optimized, properly change to sphere boundary, or do an enum to change between shapes)
                // float3 minBounds = BoidSettings.BoundaryCenter - BoidSettings.BoundarySize;
                // float3 maxBounds = BoidSettings.BoundaryCenter + BoidSettings.BoundarySize;
                //
                // float3 steer = float3.zero;
                //
                // if (currentPosition.x < minBounds.x)
                // {
                //     steer.x = BoidSettings.BoundaryWeight;
                // }
                // else if (currentPosition.x > maxBounds.x)
                // {
                //     steer.x = -BoidSettings.BoundaryWeight;
                // }
                //
                // if (currentPosition.y < minBounds.y)
                // {
                //     steer.y = BoidSettings.BoundaryWeight;
                // }
                // else if (currentPosition.y > maxBounds.y)
                // {
                //     steer.y = -BoidSettings.BoundaryWeight;
                // }
                //
                // if (currentPosition.z < minBounds.z)
                // {
                //     steer.z = BoidSettings.BoundaryWeight;
                // }
                // else if (currentPosition.z > maxBounds.z)
                // {
                //     steer.z = -BoidSettings.BoundaryWeight;
                // }
                //
                // if (!math.all(steer == float3.zero))
                // {
                //     // Apply steering to bring boid back inside the boundary
                //     currentVelocity += math.normalize(steer) * BoidSettings.BoundaryWeight * DeltaTime;
                // }

                // Update components
                SpatialEntity spatialEntity = SpatialEntities[index];
                Entity entity = spatialEntity.Entity;

                var transform = LocalTransformLookup[entity];
                transform.Position = currentPosition;
                transform.Rotation = quaternion.LookRotationSafe(currentVelocity, math.up());
                LocalTransformLookup[entity] = transform;

                var velocity = VelocityLookup[entity];
                velocity.Velocity = currentVelocity;
                VelocityLookup[entity] = velocity;
            }
        }
    }
}