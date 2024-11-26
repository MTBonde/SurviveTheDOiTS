using Unity.Entities;

namespace ECS.Components
{
    public struct WaveData : IComponentData
    {
        public int CurrentWaveCount;
        public int SpawnAmount; 
        public int TotalSpawnedBoids; 
        public int MaxBoidCount;
        public float WaveInterval;
        public int MaxAllowedThisWave;
    }
}