using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    partial struct HealthCheckSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FirstPersonPlayer>();
            state.RequireForUpdate<KilledBoidsCounter>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged); // This reuses the CB that already runs at end of simulationgroup
            
            // Get the counter
            var playerEntity = SystemAPI.GetSingletonEntity<FirstPersonPlayer>();
            var killedBoidsCounter = SystemAPI.GetComponentRW<KilledBoidsCounter>(playerEntity);
            
            foreach ((
                         RefRO<Health> health, 
                         Entity entity) 
                     in SystemAPI.Query<
                             RefRO<Health>>()
                         .WithEntityAccess())
            {
                if (health.ValueRO.HealthAmount <= 0)
                {
                    killedBoidsCounter.ValueRW.Value++;
                    entityCommandBuffer.DestroyEntity(entity); // will not result in structural changes error, as it is saved in a buffer 
                }
            }
        }
    }
}
