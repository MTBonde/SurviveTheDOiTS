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
    [UpdateAfter(typeof(WithinAttackRangeSystem))]
    partial struct CollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensure dependencies are available
            state.RequireForUpdate<FirstPersonPlayer>();
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
            foreach ((RefRW<PhysicsVelocity> velocity,
                         RefRO<BulletComponent> bulletComponent, 
                         RefRO<LocalTransform> localTransform,
                         Entity entity
                     ) in SystemAPI.Query<
                             RefRW<PhysicsVelocity>, 
                             RefRO<BulletComponent>, 
                             RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                float3 startPosition = localTransform.ValueRO.Position;

                var collisionFilter = new CollisionFilter
                {
                    BelongsTo = (uint)CollisionLayer.Bullet,
                    CollidesWith = (uint)CollisionLayer.GameWorld | (uint)CollisionLayer.Boid,
                    GroupIndex = 0
                };

                physicsWorld.CollisionWorld.SphereCastAll(startPosition, 1, float3.zero, 1,
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
            
            // Get the player's position from their LocalTransform
            var firstPersonPlayer = SystemAPI.GetSingleton<FirstPersonPlayer>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ControlledCharacter);
            float3 playerPosition = playerTransform.Position;
            
            // Check for boids within a 2-unit radius of the player and destroy them if found
            foreach ((
                         RefRO<LocalTransform> boidTransform, 
                         Entity boidEntity
                     ) in SystemAPI.Query<
                             RefRO<LocalTransform>>()
                         .WithEntityAccess()
                         .WithAll<BoidTag>())
            {
                float3 boidPosition = boidTransform.ValueRO.Position;
                float distanceToPlayer = math.distance(boidPosition, playerPosition);
            
                if (distanceToPlayer <= 2.0f)
                {
                    Debug.Log("Player is hit by boid!");
                    ecb.DestroyEntity(boidEntity);
                }
            }

            // Dispose of NativeList
            hits.Dispose();
        }
    }
}