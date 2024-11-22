using System;
using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // get the entities reference
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            
            foreach ((
                         RefRW<FirstPersonPlayerInputs> playerInputs,
                         RefRW<ShootAttackComponent> shootAttack,
                         RefRO<FirstPersonPlayer> firstPersonPlayer 
                     )in SystemAPI.Query<
                         RefRW<FirstPersonPlayerInputs>,
                         RefRW<ShootAttackComponent>,
                         RefRO<FirstPersonPlayer> 
                     >())
            {
                // EO; If the timer is greater than 0, decrement the timer by the delta time
                shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (shootAttack.ValueRO.timer >= 0)
                {
                    continue;
                }
                
                if (playerInputs.ValueRO.IsShootInputPressed)
                {
                    //Debug.Log("Shoot");
                    shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMAX;
                    

                    // Get the local transform of the player entity
                    LocalTransform playerEntityLocalTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ValueRO.ControlledCharacter);
                    
                    // Calculate the bullet spawn world position
                    float3 bulletSpawnWorldPosition = playerEntityLocalTransform.TransformPoint(shootAttack.ValueRO.bulletSpawnLocalPosition);
                    
                    // Get the character rotation and local view rotation
                    quaternion characterRotation = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ValueRO.ControlledCharacter).Rotation;
                    quaternion localCharacterViewRotation = SystemAPI.GetComponent<FirstPersonCharacterComponent>(firstPersonPlayer.ValueRO.ControlledCharacter).ViewLocalRotation;
                    
                    // Get the world view direction
                    FirstPersonCharacterUtilities.GetCurrentWorldViewDirectionAndRotation(
                        characterRotation,
                        localCharacterViewRotation,
                        out var worldCharacterViewDirection,
                        out _
                    );
                    
                    // Set the bullet direction to the world view direction
                    float3 bulletDirection = worldCharacterViewDirection;
                    
                    // Instantiate a bullet entity
                    Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.BulletPrefabEntity);
                    
                    // Set the bullet entity's local transform to the bullet spawn world position
                    SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));
                    
                    // Set the bullet entity's direction component
                    SystemAPI.SetComponent(bulletEntity, new DirectionComponent
                    {
                        Direction = bulletDirection
                    });
                }
            }
        }
    }
}