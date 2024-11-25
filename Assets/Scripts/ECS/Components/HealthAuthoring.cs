using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    public class HealthAuthoring : MonoBehaviour
    {
        public int HealthAmount;
        
        public class Baker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Health
                {
                    HealthAmount = authoring.HealthAmount,
                });
            }
        }
    }
    
    public struct Health : IComponentData
    {
        public int HealthAmount;
    }
}
