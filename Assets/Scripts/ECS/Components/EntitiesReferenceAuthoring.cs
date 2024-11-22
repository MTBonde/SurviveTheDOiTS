using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        public GameObject BulletPrefabGameObject;
        public GameObject BoidPrefabGameObject;

        public class Baker : Baker<EntitiesReferenceAuthoring>
        {
            public override void Bake(EntitiesReferenceAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntitiesReferences
                {
                    BulletPrefabEntity = GetEntity(authoring.BulletPrefabGameObject, TransformUsageFlags.Dynamic),
                    BoidPrefabEntity = GetEntity(authoring.BoidPrefabGameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    
    public struct EntitiesReferences : IComponentData
    {
        public Entity BulletPrefabEntity;
        public Entity BoidPrefabEntity;
    }
}
