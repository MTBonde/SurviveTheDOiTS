using ECS.Authoring;
using ECS.Components;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace ECS.Systems
{
    [BurstCompile]
    public partial struct BoidAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BoidSettings>();
            state.RequireForUpdate<FirstPersonPlayer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // Get the first person player entity
            var firstPersonPlayer = SystemAPI.GetSingleton<FirstPersonPlayer>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ControlledCharacter);
            float3 playerPosition = playerTransform.Position;

            // Get BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();
            
            // Reset distance for reverting to boid mode
            float resetDistance = 10f;
            float groundOffset = boidSettings.GroundOffset;
            float attackSpeed = boidSettings.MoveSpeed * 2f;

            // Query attacking boids
            foreach ((
                         RefRW<LocalTransform> localTransform, 
                         RefRW<VelocityComponent> velocityComponent, 
                         RefRW<BoidAttackComponent> boidAttackComponent,
                         Entity entity
                     ) in SystemAPI.Query<
                         RefRW<LocalTransform>, 
                         RefRW<VelocityComponent>, 
                         RefRW<BoidAttackComponent>
                     >().WithEntityAccess())
            {
                // Get boid's current position and velocity
                float3 boidPosition = localTransform.ValueRO.Position;
                float3 currentVelocity = velocityComponent.ValueRO.Velocity;

                // Calculate ground target directly below the boid
       
                float3 groundTarget = new float3(boidPosition.x, groundOffset, boidPosition.z);

                // Calculate direction to ground
                float3 directionToGround = math.normalize(groundTarget - boidPosition);

                // Calculate direction to player
                float3 directionToPlayer = math.normalize(playerPosition - boidPosition);

                // Calculate blend factor for curved movement
                float distanceToGround = math.distance(boidPosition, groundTarget);
                float groundInfluence = math.clamp(distanceToGround / 5f, 0f, 1f); // Blend ground influence based on proximity
                float3 curvedDirection = math.lerp(directionToGround, directionToPlayer, 1f - groundInfluence);

                // Smooth velocity transition for natural movement
                float3 smoothedVelocity = math.lerp(currentVelocity, curvedDirection * attackSpeed, 0.2f);

                // Update boid's position and velocity
                velocityComponent.ValueRW.Velocity = smoothedVelocity;
                localTransform.ValueRW.Position += smoothedVelocity * SystemAPI.Time.DeltaTime;

                // Check if boid should reset (player is too far)
                if (math.distance(boidPosition, playerPosition) > resetDistance && distanceToGround < 0.5f)
                {
                    boidAttackComponent.ValueRW.IsAttacking = false; // Reset to boid mode
                    entityCommandBuffer.SetComponentEnabled<BoidAttackComponent>(entity, false);
                    return;
                }

                // Update rotation to face movement direction
                localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(smoothedVelocity, math.up());
            }
        }
    }
}