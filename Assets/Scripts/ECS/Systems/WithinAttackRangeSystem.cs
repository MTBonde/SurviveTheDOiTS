using ECS.Authoring;
using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

namespace ECS.Systems
{
    [BurstCompile]
    public partial struct WithinAttackRangeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<FirstPersonPlayer>();
            state.RequireForUpdate<BoidSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged); // This reuses the CB that already runs at end of simulationgroup
            
            float speed = SystemAPI.GetSingleton<BoidSettings>().MoveSpeed;
            
            // Get the player's position from their LocalTransform
            var firstPersonPlayer = SystemAPI.GetSingleton<FirstPersonPlayer>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ControlledCharacter);
            float3 playerPosition = playerTransform.Position;

            // Get the attack radius from BoidSettings
            float attackRadius = SystemAPI.GetSingleton<BoidSettings>().AttackRange;

            // Random seed for coin flip logic
            uint randomSeed = (uint)UnityEngine.Time.frameCount;
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(randomSeed);

            // Iterate over all boids
            foreach ((
                         RefRO<LocalTransform> localTransform, 
                         RefRO<BoidTag> boid,
                         Entity entity
                     ) in SystemAPI.Query<
                             RefRO<LocalTransform>, 
                             RefRO<BoidTag>
                         >()
                         .WithEntityAccess())
            {
                float3 boidPosition = localTransform.ValueRO.Position;

                // Check distance to player
                if (math.distance(boidPosition, playerPosition) <= attackRadius)
                {
                    // Coin flip: 50% chance to switch to attacking
                    if (random.NextFloat() < 0.5f)
                    {
                        // Debug.Log("Boid is attacking!");
                        // RefRW<MoveSpeedComponent> moveSpeedComponent = SystemAPI.GetComponentRW<MoveSpeedComponent>(entity);
                        // moveSpeedComponent.ValueRW.Speed = speed * 2;
                        
                        entityCommandBuffer.SetComponent(entity, new BoidAttackComponent
                        {
                            IsAttacking = true,
                        });
                        
                        entityCommandBuffer.SetComponentEnabled<BoidAttackComponent>(entity, true);
                    }
                }
            }
        }
    }
}