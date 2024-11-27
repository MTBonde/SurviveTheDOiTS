// using System;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
//
// namespace SpatialHashmap
// {
//     public struct SpatialEntity
//     {
//         public float3 Position; 
//         public Entity Entity;   
//     }
//     
//     public struct HashAndIndex : IComparable<HashAndIndex>
//     {
//         public int Hash;  
//         public int Index; 
//
//         public int CompareTo(HashAndIndex other) => Hash.CompareTo(other.Hash);
//     }
//
//     public static class SpatialHashMapHelper
//     {
//         public static int Hash(int3 gridPos)
//         {
//             unchecked
//             {
//                 return 
//                     (gridPos.x * 73856093) ^ 
//                     (gridPos.y * 19349663) ^ 
//                     (gridPos.z * 83492791);
//             }
//         }
//
//         public static int3 GetGridPosition(float3 position, float cellSize)
//         {
//             return new int3(math.floor(position / cellSize));
//         }
//     }
//
//     [BurstCompile]
//     public struct HashEntitiesJob : IJobParallelFor
//     {
//         [ReadOnly] public NativeArray<SpatialEntity> Entities;
//         public NativeArray<HashAndIndex> HashAndIndices;
//         public float CellSize;
//
//         public void Execute(int index)
//         {
//             var entity = Entities[index];
//             int3 gridPos = SpatialHashMapHelper.GetGridPosition(entity.Position, CellSize);
//             int hash = SpatialHashMapHelper.Hash(gridPos);
//
//             HashAndIndices[index] = new HashAndIndex { Hash = hash, Index = index };
//         }
//     }
//
//     [BurstCompile]
//     public struct SortHashCodesJob : IJob
//     {
//         public NativeArray<HashAndIndex> HashAndIndices;
//
//         public void Execute()
//         {
//             HashAndIndices.Sort();
//         }
//     }
//
//     [BurstCompile]
//     public struct QueryNeighborsJob : IJob
//     {
//         [ReadOnly] public NativeArray<SpatialEntity> Entities;
//         [ReadOnly] public NativeArray<HashAndIndex> HashAndIndices;
//         public NativeList<int> NeighborIndices;
//         public float3 QueryPosition;
//         public float QueryRadius;
//         public float CellSize;
//
//         public void Execute()
//         {
//             NeighborIndices.Clear();
//             float radiusSquared = QueryRadius * QueryRadius;
//
//             int3 minGridPos = SpatialHashMapHelper.GetGridPosition(QueryPosition - QueryRadius, CellSize);
//             int3 maxGridPos = SpatialHashMapHelper.GetGridPosition(QueryPosition + QueryRadius, CellSize);
//
//             for (int x = minGridPos.x; x <= maxGridPos.x; x++)
//             {
//                 for (int y = minGridPos.y; y <= maxGridPos.y; y++)
//                 {
//                     for (int z = minGridPos.z; z <= maxGridPos.z; z++)
//                     {
//                         int hash = SpatialHashMapHelper.Hash(new int3(x, y, z));
//                         int startIndex = BinarySearchFirst(HashAndIndices, hash);
//
//                         if (startIndex < 0) continue;
//
//                         for (int i = startIndex; i < HashAndIndices.Length && HashAndIndices[i].Hash == hash; i++)
//                         {
//                             int entityIndex = HashAndIndices[i].Index;
//                             float3 entityPosition = Entities[entityIndex].Position;
//
//                             if (math.distancesq(entityPosition, QueryPosition) <= radiusSquared)
//                             {
//                                 NeighborIndices.Add(entityIndex);
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//
//         private int BinarySearchFirst(NativeArray<HashAndIndex> array, int hash)
//         {
//             int left = 0, right = array.Length - 1;
//             while (left <= right)
//             {
//                 int mid = (left + right) / 2;
//                 if (array[mid].Hash == hash) return mid;
//                 if (array[mid].Hash < hash) left = mid + 1;
//                 else right = mid - 1;
//             }
//             
//             return -1;
//         }
//     }
// }