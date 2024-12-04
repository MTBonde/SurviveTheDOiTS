using ECS.Authoring;
using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            // Create an EntityCommandBuffer for deferred operations
            EntityCommandBuffer entityCommandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Get the player entity and its position
            var firstPersonPlayer = SystemAPI.GetSingleton<FirstPersonPlayer>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ControlledCharacter);
            float3 playerPosition = playerTransform.Position;

            // Get BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();

            // Constants for attack logic
            float resetDistance = boidSettings.AttackRange / 2f;
            float attackSpeed = boidSettings.MoveSpeed * 1.5f;

            // Query all boids that are in attack mode
            foreach ((
                         RefRW<LocalTransform> localTransform, 
                         RefRW<BoidAttackComponent> boidAttackComponent, 
                         RefRW<DirectionComponent> directionComponent, 
                         RefRW<MoveSpeedComponent> moveSpeedComponent, 
                         Entity entity
                     ) in SystemAPI.Query<
                             RefRW<LocalTransform>, 
                             RefRW<BoidAttackComponent>, 
                             RefRW<DirectionComponent>, 
                             RefRW<MoveSpeedComponent>>()
                         .WithEntityAccess())
            {
                float3 boidPosition = localTransform.ValueRO.Position;

                // Calculate the direct direction to the player
                float3 directToPlayer = math.normalize(playerPosition - boidPosition);

                // Calculate a curve by adding an offset perpendicular to the direction
                float3 perpendicularOffset = math.cross(directToPlayer, new float3(0, 1, 0)) * 0.5f;

                // Blend between direct approach and curved path
                float3 curvedDirection = math.normalize(directToPlayer + perpendicularOffset);

                // Smooth transition to avoid sharp turns
                float3 currentDirection = directionComponent.ValueRO.Direction;
                float3 smoothedDirection = math.lerp(currentDirection, curvedDirection, 0.2f); // Adjust factor as needed

                // Update direction and speed components
                directionComponent.ValueRW.Direction = smoothedDirection;
                moveSpeedComponent.ValueRW.Speed = attackSpeed;

                // Move the boid toward the player
                localTransform.ValueRW.Position += smoothedDirection * attackSpeed * SystemAPI.Time.DeltaTime;

                // Check if the boid should exit attack mode
                if (math.distance(boidPosition, playerPosition) > resetDistance)
                {
                    entityCommandBuffer.SetComponentEnabled<BoidAttackComponent>(entity, false);
                    boidAttackComponent.ValueRW.IsAttacking = false;
                }
            }
        }
    }
}