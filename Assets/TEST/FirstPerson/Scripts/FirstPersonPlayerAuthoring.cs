using ECS.Components;
using UnityEngine;
using Unity.Entities;

[DisallowMultipleComponent]
public class FirstPersonPlayerAuthoring : MonoBehaviour
{
    public GameObject ControlledCharacter;
    public float MouseSensitivity = 1.0f;
    public float FireRate = 0.1f;

    public class Baker : Baker<FirstPersonPlayerAuthoring>
    {
        public override void Bake(FirstPersonPlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new FirstPersonPlayer
            {
                ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                MouseSensitivity = authoring.MouseSensitivity,
                FireRate = authoring.FireRate
            });
            AddComponent<FirstPersonPlayerInputs>(entity);
            AddComponent<ShootAttack>(entity);
        }
    }
}