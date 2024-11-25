using ECS.Components;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Mono.UI
{
    public class TopBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI killedBoidsText;
        [SerializeField] private TextMeshProUGUI currentBoidsText;

        private void Update()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BoidTag>().Build(entityManager);
            
            currentBoidsText.text = entityQuery.CalculateEntityCount().ToString();
            entityQuery.Dispose();
            
            // Get killed boids counter
            var counterEntity = entityManager.CreateEntityQuery(typeof(KilledBoidsCounter)).GetSingletonEntity();
            var killedBoidsCounter = entityManager.GetComponentData<KilledBoidsCounter>(counterEntity);
            killedBoidsText.text = killedBoidsCounter.Value.ToString();
        }
    }
}