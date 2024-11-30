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
            state.RequireForUpdate<BoidSettings>();
            state.RequireForUpdate<FirstPersonPlayer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get the player entity and its position
            var firstPersonPlayer = SystemAPI.GetSingleton<FirstPersonPlayer>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ControlledCharacter);
            float3 playerPosition = playerTransform.Position;

            // Get BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();

            // Constants for attack logic
            float resetDistance = 10f;
            float groundOffset = boidSettings.GroundOffset;
            float attackSpeed = boidSettings.MoveSpeed * 2f;

            // Query attacking boids
            foreach ((
                         RefRW<LocalTransform> localTransform, 
                         RefRW<BoidAttackComponent> boidAttackComponent,
                         RefRW<DirectionComponent> directionComponent,
                         RefRW<MoveSpeedComponent> moveSpeedComponent
                     ) in SystemAPI.Query<
                         RefRW<LocalTransform>, 
                         RefRW<BoidAttackComponent>, 
                         RefRW<DirectionComponent>, 
                         RefRW<MoveSpeedComponent>>())
            {
                float3 boidPosition = localTransform.ValueRO.Position;

                // Calculate ground target below the boid
                float3 groundTarget = new float3(boidPosition.x, groundOffset, boidPosition.z);

                // Calculate directions for curve calculation
                float3 directionToGround = math.normalize(groundTarget - boidPosition);
                float3 directionToPlayer = math.normalize(playerPosition - boidPosition);

                // Calculate the curved direction
                float distanceToGround = math.distance(boidPosition, groundTarget);
                float groundInfluence = math.clamp(distanceToGround / 5f, 0f, 1f); // Influence of ground proximity
                float3 curvedDirection = math.lerp(directionToGround, directionToPlayer, 1f - groundInfluence);

                // Update the direction and speed components
                directionComponent.ValueRW.Direction = curvedDirection;
                moveSpeedComponent.ValueRW.Speed = attackSpeed;

                // Check if the boid should reset to normal mode
                if (math.distance(boidPosition, playerPosition) > resetDistance && distanceToGround < 0.5f)
                {
                    boidAttackComponent.ValueRW.IsAttacking = false; // Reset attack mode
                    return;
                }
            }
        }
    }
}
