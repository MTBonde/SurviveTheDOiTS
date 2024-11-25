using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    partial struct LifeTimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>(); }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((
                         RefRW<LifetimeComponent> lifetimeComponent,
                         Entity entity
                     ) in SystemAPI.Query<
                             RefRW<LifetimeComponent>
                         >()
                         .WithEntityAccess())
            {
                // Decrease remaining lifetime
                lifetimeComponent.ValueRW.RemainingLifetime -= deltaTime;

                // Check if lifetime has expired
                if (lifetimeComponent.ValueRO.RemainingLifetime <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}