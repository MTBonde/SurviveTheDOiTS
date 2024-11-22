using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace ECS.Systems
{
    partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                         RefRW<PhysicsVelocity> physicsVelocity,
                         RefRO<MoveSpeedComponent> moveSpeedComponent, 
                         RefRO<DirectionComponent> directionComponent
                     ) in SystemAPI.Query<
                         RefRW<PhysicsVelocity>,
                         RefRO<MoveSpeedComponent>, 
                         RefRO<DirectionComponent>
                     >())
            {
                float3 direction = math.normalize(directionComponent.ValueRO.Direction);
                float moveSpeed = moveSpeedComponent.ValueRO.Speed;
                
                physicsVelocity.ValueRW.Linear = moveSpeed * direction;
                //localTransform.ValueRW.Position += direction * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}