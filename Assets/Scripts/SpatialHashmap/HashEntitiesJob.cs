using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SpatialHashmap
{
    [BurstCompile]
    public struct HashEntitiesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<SpatialEntity> Entities;
        public NativeArray<HashAndIndex> HashAndIndices;
        public float CellSize;

        public void Execute(int index)
        {
            var entity = Entities[index];
            int3 gridPos = SpatialHashMapHelper.GetGridPosition(entity.Position, CellSize);
            int hash = SpatialHashMapHelper.Hash(gridPos);

            HashAndIndices[index] = new HashAndIndex { Hash = hash, Index = index };
        }
    }
}