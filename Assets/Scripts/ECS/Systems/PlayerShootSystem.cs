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
            state.RequireForUpdate<EntitiesReference>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // get the entities reference
            EntitiesReference entitiesReference = SystemAPI.GetSingleton<EntitiesReference>();
            //Entity playerEntity = SystemAPI.GetSingletonEntity<FirstPersonPlayer>();
            
            foreach ((
                         RefRW<FirstPersonPlayer> firstPersonPlayer, 
                         RefRW<FirstPersonPlayerInputs> playerInputs,
                         RefRW<ShootAttack> shootAttack
                     )in SystemAPI.Query<
                         RefRW<FirstPersonPlayer>, 
                         RefRW<FirstPersonPlayerInputs>,
                         RefRW<ShootAttack>
                     >())
            {
                // // EO; If the timer is greater than 0, decrement the timer by the delta time
                // shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                // if (shootAttack.ValueRO.timer > 0)
                // {
                //     continue;
                // }
                // shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMAX;
                
                //if (playerInputs.ValueRO.IsShootInputPressed && firstPersonPlayer.ValueRW.FireRate <= 0)
                if (playerInputs.ValueRO.IsShootInputPressed)
                {
                    Debug.Log("Shoot");
                    // Instantiate a bullet entity
                    Entity bulletEntity = state.EntityManager.Instantiate(entitiesReference.BulletPrefabEntity);
                    Debug.Log($"Bullet instantiated: {bulletEntity}");
                    
                    // // Get the local transform of the player entity
                    // LocalTransform playerEntityLocalTransform = SystemAPI.GetComponent<LocalTransform>(firstPersonPlayer.ValueRO.ControlledCharacter);
                    //
                    // // Calculate the bullet spawn world position
                    // float3 bulletSpawnWorldPosition = playerEntityLocalTransform.TransformPoint(shootAttack.ValueRO.bulletSpawnLocalPosition);
                    //
                    // // Set the bullet entity's local transform to the bullet spawn world position
                    // SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));
                }
            }
        }
    }
}