using SpatialHashmap;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Collects entities and transforms into a spatial entity array.
    /// </summary>
    [BurstCompile]
    public struct CollectSpatialEntitiesJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<SpatialEntity> SpatialEntities;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<LocalTransform> Positions;

        public void Execute(int index)
        {
            SpatialEntities[index] = new SpatialEntity
            {
                Position = Positions[index].Position,
                Entity = Entities[index]
            };
        }
    }
}