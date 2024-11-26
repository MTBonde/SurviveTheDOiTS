using ECS.Components;
using ECS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// Authoring class to define settings for boid spawning, such as the boid count.
    /// This class makes boid settings accessible in the Unity Inspector.
    /// </summary>
    public class BoidSpawnerAuthoring : MonoBehaviour
    {
        // Set the number of boids to spawn in the Inspector.
        public int MaxBoidCount = 20000;
        public GameObject SpawnPosition;

        /// <summary>
        /// Baker class that converts authoring component data into ECS components for boid spawning settings.
        /// This class ensures that settings defined in BoidSpawnerAuthoring are baked into the ECS world.
        /// </summary>
        private class BoidSettingsBaker : Baker<BoidSpawnerAuthoring>
        {
            public override void Bake(BoidSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSpawner
                {
                    MaxBoidCount = authoring.MaxBoidCount,
                    SpawnPosition = authoring.SpawnPosition.transform.position
                });
                
                AddComponent(entity, new WaveData
                {
                    CurrentWaveCount = 1,
                    SpawnAmount = 2,
                    MaxBoidCount = authoring.MaxBoidCount,
                    WaveInterval = 5f
                });
            }
        }
    }

    /// <summary>
    /// Data component for boid spawning settings, such as the boid count.
    /// </summary>
    public struct BoidSpawner : IComponentData
    {
        public int MaxBoidCount;
        public float3 SpawnPosition;
    }
}