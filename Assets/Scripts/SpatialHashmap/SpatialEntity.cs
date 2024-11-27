using Unity.Entities;
using Unity.Mathematics;

namespace SpatialHashmap
{
    public struct SpatialEntity
    {
        public float3 Position; 
        public Entity Entity;   
    }
}