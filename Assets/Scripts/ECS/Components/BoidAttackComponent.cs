using Unity.Entities;

namespace ECS.Components
{
    public struct BoidAttackComponent : IComponentData, IEnableableComponent
    {
        public bool IsAttacking; 
    }
}