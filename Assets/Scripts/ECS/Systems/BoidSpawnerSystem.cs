using ECS.Components;
using ECS.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// System responsible for spawning boids based on wave data and spawner settings.
    /// </summary>
    [UpdateAfter(typeof(WaveManagerSystem))]
    public partial struct BoidSpawnerSystem : ISystem
    {
        private EntityQuery _boidQuery;
        private float _timeSinceLastSpawn;

        /// <summary>
        /// Initializes required queries and state for the system.
        /// </summary>
        /// <param name="state">System state.</param>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
            state.RequireForUpdate<WaveData>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<BoidSpawner>();

            _boidQuery = state.GetEntityQuery(ComponentType.ReadOnly<BoidTag>());
            _timeSinceLastSpawn = 0f;
        }

        /// <summary>
        /// Executes boid spawning logic during each frame update.
        /// </summary>
        /// <param name="state">System state.</param>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Retrieve singleton data
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();
            RefRW<WaveData> waveData = SystemAPI.GetSingletonRW<WaveData>();

            // Update spawn timer
            _timeSinceLastSpawn += SystemAPI.Time.DeltaTime;

            // Spawn interval in seconds
            const float spawnInterval = 4.0f;

            // Check spawn timing
            if (_timeSinceLastSpawn < spawnInterval) return;

            _timeSinceLastSpawn = 0f; // Reset timer

            // Current number of boids
            int currentBoidCount = _boidQuery.CalculateEntityCount();

            // Determine wave limits
            int maxAllowedThisWave = waveData.ValueRO.MaxAllowedThisWave;

            // Random generator for position offsets
            Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            // Spawn boids for each spawner
            foreach (
                RefRO<BoidSpawner> boidSpawner 
                in SystemAPI.Query<
                    RefRO<BoidSpawner>
                >())
            {
                int maxBoidCount = boidSpawner.ValueRO.MaxBoidCount;

                // Calculate boids to spawn for this spawner
                int boidsToSpawn = math.min(maxAllowedThisWave - currentBoidCount, maxBoidCount - currentBoidCount);
                
                // Skip if no boids can be spawned
                if (boidsToSpawn <= 0) continue; 
                
                // if boids to spawn is over 1024 limit to 1024
                if (boidsToSpawn > 1024) boidsToSpawn = 1024;

                // Exit early if limits are reached
                if (currentBoidCount >= maxAllowedThisWave) return;
                
                for (int i = 0; i < boidsToSpawn; i++)
                {
                    // Spawn boid entity
                    Entity boidEntity = state.EntityManager.Instantiate(entitiesReferences.BoidPrefabEntity);

                    // Calculate random spawn position relative to the spawner
                    float3 positionOffset = random.NextFloat3(-10f, 10f);
                    float3 spawnPosition = boidSpawner.ValueRO.SpawnPosition + positionOffset;
                    state.EntityManager.SetComponentData(boidEntity, LocalTransform.FromPosition(spawnPosition));

                    // Generate random target position within boundary
                    float3 targetOffset = random.NextFloat3(-10f, 10f);
                    float3 targetPosition = boidSettings.BoundaryCenter + targetOffset;

                    // Calculate direction from spawn to target
                    float3 direction = math.normalize(targetPosition - spawnPosition);
                    state.EntityManager.SetComponentData(boidEntity, new DirectionComponent { Direction = direction });

                    // Assign random movement speed
                    float speed = random.NextFloat(1f, 3f);
                    state.EntityManager.SetComponentData(boidEntity, new MoveSpeedComponent { Speed = speed });

                    // Update the current boid count
                    currentBoidCount++;
                }
            }
        }
    }
}
