using ECS.Components;
using Unity.Entities;

namespace ECS.Systems
{
    public partial struct WaveManagerSystem : ISystem
    {
        private float timeSinceLastWave;
        private int waveCount;
        private const int MAX_WAVES = 20;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveData>();
            waveCount = 1;
            timeSinceLastWave = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            var waveData = SystemAPI.GetSingletonRW<WaveData>();
            timeSinceLastWave += SystemAPI.Time.DeltaTime;

            if (timeSinceLastWave >= waveData.ValueRO.WaveInterval && waveCount <= MAX_WAVES)
            {
                timeSinceLastWave = 0f;

                // Calculate the max allowed boids for this wave (2 * 2^(waveCount - 1))
                int maxAllowedThisWave = 16 * (2 << waveCount);

                // Update WaveData with the max allowed on-screen
                waveData.ValueRW.MaxAllowedThisWave = maxAllowedThisWave;

                // Increment wave count
                if (waveCount < MAX_WAVES)
                {
                    waveData.ValueRW.CurrentWaveCount = waveCount++;
                }
            }
        }
    }
}