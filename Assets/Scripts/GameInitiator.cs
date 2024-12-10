using System;
using System.Threading.Tasks;
using Disposables;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameInitiator : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Light _mainDirectionalLight;
    [SerializeField] private EventSystem _mainEventSystem;
    [SerializeField] private GameObject _background;
    //[SerializeField] private EnemySpawner _enemiesSpawner;
    [SerializeField] private Canvas _mainCanvas;
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _loadingScreen;
    private async void Start()
    {
        Debug.Log("Starting game initialization...");
        BindObjects();
        await Task.Delay(1000);
        using (var loadingScreenDisposable = new LoadingScreenDisposable(_loadingScreen))
        {
            loadingScreenDisposable.SetLoadingBarPercent(0);
            //await InitializeGame();
            
            // wait 1 second
            await Task.Delay(1000);
            
            loadingScreenDisposable.SetLoadingBarPercent(0.25f);
            //await InitializeObjects();
            
            await Task.Delay(1000);
            loadingScreenDisposable.SetLoadingBarPercent(0.5f);
            await CreateObjects();
            
            await Task.Delay(1000);
            loadingScreenDisposable.SetLoadingBarPercent(0.75f);
            PrepareGame();
            
            await Task.Delay(1000);
            loadingScreenDisposable.SetLoadingBarPercent(0.99f);
        }

        await StartGame();
    }


    private void BindObjects()
    {
        Debug.Log("Binding objects...");
        _mainCamera = Instantiate(_mainCamera);
        _mainDirectionalLight = Instantiate(_mainDirectionalLight);
        _mainEventSystem = Instantiate(_mainEventSystem);
        _mainCanvas = Instantiate(_mainCanvas);
        
        // Find and assign the LoadingScreenPanel within the MainCanvas
        _loadingScreen = _mainCanvas.transform.Find("LoadingScreenPanel").gameObject;
    }

    private async Task InitializeGame() { throw new NotImplementedException(); }
    
    private async Task InitializeObjects()
    {
        throw new NotImplementedException();
    }
    

    /// <summary>
    /// Loads essential subscenes and prepares entities for the scene.
    /// </summary>
    private async Task CreateObjects()
    {
        Debug.Log("Creating essential entities and loading subscenes...");
    
        
        // var playerSubscene = FindObjectOfType<SubScene>();
        // if (playerSubscene == null)
        // {
        //     Debug.LogError("Player subscene not found!");
        //     return;
        // }
        //
        // // Load the subscene asynchronously and wait until it's fully loaded.
        // while (!playerSubscene.IsLoaded)
        // {
        //     await Task.Delay(100); // Check every 100ms if subscene has loaded
        // }

        // After the subscene is loaded, retrieve the player entity.
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Use an EntityQuery to locate the third-person player entity in the subscene.
        var playerQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FirstPersonPlayer>());
        if (playerQuery.CalculateEntityCount() > 0)
        {
            Entity playerEntity = playerQuery.GetSingletonEntity();
            Debug.Log("Player entity found and ready.");
        }
        else
        {
            Debug.LogError("Player entity not found in subscene!");
        }
    
        await Task.CompletedTask;
    }


    private void PrepareGame()
    {
        _player.SetActive(true);
        // _player.MoveToRandomPosition();
        // _player.SetStartingWeapon();
        //
        // _enemiesSpawner.SetSpawnPoints();
        // _enemiesSpawner.HideAllEnemies();
        //
        // _levelUI.UpdateLevelText(_levelmanager.CurrentLevel.index);
    }

    private async Task StartGame()
    {
        // await _levelUI.ShowLevelAnimation();
        // _enemiesSpawner.StartSpawning();
    }
}