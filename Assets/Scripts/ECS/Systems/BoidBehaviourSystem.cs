using ECS.Authoring;
using ECS.Components;
using SpatialHashmap;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

            // Allocate arrays for spatial data
            using (var spatialEntities = new NativeArray<SpatialEntity>(boidCount, Allocator.TempJob))
            using (var hashAndIndices = new NativeArray<HashAndIndex>(boidCount, Allocator.TempJob))
            using (var neighborIndices = new NativeList<int>(Allocator.TempJob))
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
                    NeighborIndices = neighborIndices,
                    QueryRadius = boidSettings.NeighborRadius,
                    CellSize = boidSettings.NeighborRadius
                };
                JobHandle queryNeighboringBoidsJobHandle = queryNeighboringBoidsJob.Schedule(sortJobHandle);
                queryNeighboringBoidsJobHandle.Complete();

                // Perform boid behavior using neighbor data
                var boidBehaviorJob = new BoidBehaviorJob
                {
                    DeltaTime = deltaTime,
                    BoidSettings = boidSettings,
                    SpatialEntities = spatialEntities,
                    NeighborIndices = neighborIndices.AsArray(),
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
            [ReadOnly] public NativeArray<int> NeighborIndices;

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

                // Loop over neighbors only
                for (int i = 0; i < NeighborIndices.Length; i++)
                {
                    int neighborIndex = NeighborIndices[i];
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