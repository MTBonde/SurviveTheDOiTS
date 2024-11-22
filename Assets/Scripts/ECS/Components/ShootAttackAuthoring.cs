using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Components
{
    public class ShootAttackAuthoring : MonoBehaviour
    {
        public float timerMAX;
        public int damageAmount;
        public float attackRange;
        public Transform bulletSpawnPositionTransform;
        
        public class Baker : Baker<ShootAttackAuthoring>
        {
            public override void Bake(ShootAttackAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShootAttackComponent
                {
                    timerMAX = authoring.timerMAX,
                    damageAmount = authoring.damageAmount,
                    attackRange = authoring.attackRange,
                    bulletSpawnLocalPosition = authoring.bulletSpawnPositionTransform.localPosition
                });
            }
        }
    }
    
    public struct ShootAttackComponent : IComponentData
    {
        public float timer;
        public float timerMAX;
        public int damageAmount;
        public float attackRange;
        public float3 bulletSpawnLocalPosition; 
    }
}
