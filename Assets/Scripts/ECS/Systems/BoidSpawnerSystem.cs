using ECS.Components;
using ECS.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    [UpdateAfter(typeof(WaveManagerSystem))]
    public partial struct BoidSpawnerSystem : ISystem
    {
        private EntityQuery _boidQuery;
        private float timeSinceLastSpawn;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
            state.RequireForUpdate<WaveData>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<BoidSpawner>();

            _boidQuery = state.GetEntityQuery(ComponentType.ReadOnly<BoidTag>());
            timeSinceLastSpawn = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var waveData = SystemAPI.GetSingletonRW<WaveData>();
            var boidSpawner = SystemAPI.GetSingleton<BoidSpawner>();
            var boidSettings = SystemAPI.GetSingleton<BoidSettings>();
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

            // Update spawn timer
            timeSinceLastSpawn += SystemAPI.Time.DeltaTime;

            // Define spawn interval (adjust as needed)
            float spawnInterval = 4.0f; // Spawn every 4 seconds

            // Check if it's time to spawn
            if (timeSinceLastSpawn < spawnInterval) return;

            timeSinceLastSpawn = 0f; // Reset the timer

            // Current boid count
            int currentBoidCount = _boidQuery.CalculateEntityCount();

            // Max allowed boids for this wave
            int maxAllowedThisWave = waveData.ValueRO.MaxAllowedThisWave;

            // Max overall boid count
            int maxBoidCount = boidSpawner.MaxBoidCount;

            // Calculate how many boids to spawn in this batch
            int boidsToSpawn = math.min(maxAllowedThisWave - currentBoidCount, maxBoidCount - currentBoidCount);

            if (boidsToSpawn <= 0) return; // Nothing to spawn if limits are reached

            // Spawn boids
            Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
            for (int i = 0; i < boidsToSpawn; i++)
            {
                if (currentBoidCount >= maxBoidCount || currentBoidCount >= maxAllowedThisWave) break;

                Entity boidEntity = state.EntityManager.Instantiate(entitiesReferences.BoidPrefabEntity);

                float3 randomOffset = random.NextFloat3(-10f, 10f);
                float3 boidSpawnPosition = boidSpawner.SpawnPosition + randomOffset;
                state.EntityManager.SetComponentData(boidEntity, LocalTransform.FromPosition(boidSpawnPosition));

                var direction = (boidSettings.BoundaryCenter - boidSpawnPosition) + randomOffset;
                
                var targetVelocity = math.normalize(direction) * boidSettings.MoveSpeed;
                state.EntityManager.SetComponentData(boidEntity, new VelocityComponent { Velocity = targetVelocity });

                currentBoidCount++;
            }
        }
    }
}
