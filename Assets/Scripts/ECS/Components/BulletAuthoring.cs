using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Components
{
    public class BulletAuthoring : MonoBehaviour
    {
        public float BulletSpeed;
        public int DamageAmount;
        
        public class Baker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BulletComponent
                {
                    DamageAmount = authoring.DamageAmount,
                    BulletSize = 0.1f
                });
                AddComponent(entity, new MoveSpeedComponent
                {
                    Speed = authoring.BulletSpeed
                });
                AddComponent(entity, new DirectionComponent
                {
                    Direction = new float3(0, 0, 1)
                });
            }
        }
    }
    
    public struct BulletComponent : IComponentData
    {
        public int DamageAmount;
        public float BulletSize;
    }
}
