using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        public GameObject BulletPrefabGameObject;
        public GameObject ZombiePrefabGameObject;
        public GameObject SoldierPrefabGameObject;

        public class Baker : Baker<EntitiesReferenceAuthoring>
        {
            public override void Bake(EntitiesReferenceAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntitiesReference
                {
                    BulletPrefabEntity = GetEntity(authoring.BulletPrefabGameObject, TransformUsageFlags.Dynamic),
                    ZombiePrefabEntity = GetEntity(authoring.ZombiePrefabGameObject, TransformUsageFlags.Dynamic),
                    SoldierPrefabEntity = GetEntity(authoring.SoldierPrefabGameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    
    public struct EntitiesReference : IComponentData
    {
        public Entity BulletPrefabEntity;
        public Entity ZombiePrefabEntity;
        public Entity SoldierPrefabEntity;
    }
}
