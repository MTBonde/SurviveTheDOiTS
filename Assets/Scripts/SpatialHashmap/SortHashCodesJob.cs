using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SpatialHashmap
{
    [BurstCompile]
    public struct SortHashCodesJob : IJob
    {
        public NativeArray<HashAndIndex> HashAndIndices;

        public void Execute()
        {
            HashAndIndices.Sort();
        }
    }
}