using Unity.Entities;

namespace ECS.Components
{
    public struct KilledBoidsCounter : IComponentData
    {
        public int Value;
    }
}