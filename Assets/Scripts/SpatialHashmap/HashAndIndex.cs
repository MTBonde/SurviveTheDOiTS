using System;

namespace SpatialHashmap
{
    public struct HashAndIndex : IComparable<HashAndIndex>
    {
        public int Hash;  
        public int Index; 

        public int CompareTo(HashAndIndex other) => Hash.CompareTo(other.Hash);
    }
}