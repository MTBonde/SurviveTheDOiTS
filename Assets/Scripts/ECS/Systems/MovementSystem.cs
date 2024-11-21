using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                         RefRW<MoveSpeedComponent> moveSpeedComponent, 
                         RefRW<DirectionComponent> directionComponent,
                         RefRW<LocalTransform> localTransform
                     ) in SystemAPI.Query<
                         RefRW<MoveSpeedComponent>, 
                         RefRW<DirectionComponent>,
                         RefRW<LocalTransform>
                     >())
            {
                float3 direction = math.normalize(directionComponent.ValueRW.Direction);
                float moveSpeed = moveSpeedComponent.ValueRW.Speed;
                
                localTransform.ValueRW.Position += direction * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}