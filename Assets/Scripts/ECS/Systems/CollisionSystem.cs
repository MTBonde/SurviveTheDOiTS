using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    partial struct CollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensure dependencies are available
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Fetch physics world and command buffer
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Create a reusable NativeList for hit results
            var hits = new NativeList<ColliderCastHit>(Allocator.Temp);

            // Query all entities with BulletComponent and LocalTransform
            foreach ((
                         RefRW<PhysicsVelocity> velocity,
                         RefRO<BulletComponent> bulletComponent, 
                         RefRO<LocalTransform> localTransform,
                         Entity entity
                     )in SystemAPI.Query<
                             RefRW<PhysicsVelocity>,
                             RefRO<BulletComponent>, 
                             RefRO<LocalTransform>
                         >()
                         .WithEntityAccess()) 
            {
                float3 startPosition = localTransform.ValueRO.Position;
                //float sphereRadius = bulletComponent.ValueRO.BulletSize;

                var collisionFilter = new CollisionFilter
                {
                    BelongsTo = (uint)CollisionLayer.Bullet,
                    CollidesWith = (uint)CollisionLayer.GameWorld | (uint)CollisionLayer.Boid,
                    GroupIndex = 0
                };

                physicsWorld.CollisionWorld.SphereCastAll(startPosition, 0.5f, float3.zero, 1,
                    ref hits, collisionFilter);

                // Process hits sorted by layer
                foreach (var hit in hits)
                {
                    // Access the rigid body of the hit
                    var rigidBody = physicsWorld.PhysicsWorld.Bodies[hit.RigidBodyIndex];
                    var hitFilter = rigidBody.Collider.Value.GetCollisionFilter(hit.ColliderKey);

                    // Check if the hit entity belongs to the GameWorld layer
                    if ((hitFilter.BelongsTo & (uint)CollisionLayer.GameWorld) != 0)
                    {
                        Debug.Log("Bullet hit wall");
                        
                        // Destroy the bullet entity
                        ecb.DestroyEntity(entity);
                    }
                    
                    if ((hitFilter.BelongsTo & (uint)CollisionLayer.Boid) != 0)
                    {
                        Debug.Log("Bullet hit boid");
                        
                        // reduce health of boid
                        RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(hit.Entity);
                        targetHealth.ValueRW.HealthAmount -= bulletComponent.ValueRO.DamageAmount;
                        
                        // Destroy the bullet entity
                        ecb.DestroyEntity(entity);
                    }
                }
                
                hits.Clear();
            }
            
            // Dispose of NativeList
            hits.Dispose();
        }
    }
}