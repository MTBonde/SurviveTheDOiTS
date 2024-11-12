using System;
using System.Threading.Tasks;
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
        BindObjects();
        // using (var loadingScreenDisposable = new ShowLoadingScreenDisposable(_loadingScreen))
        // {
        //     loadingScreenDisposable.SetLoadingBarPercent(0);
        //     await InitializeGame();
        //     loadingScreenDisposable.SetLoadingBarPercent(0.25f);
        //     await InitializeObjects();
        //     loadingScreenDisposable.SetLoadingBarPercent(0.5f);
        //     await CreateObjects();
        //     loadingScreenDisposable.SetLoadingBarPercent(0.75f);
        //     PrepareGame();
        //     loadingScreenDisposable.SetLoadingBarPercent(1);
        // }

        await StartGame();
    }


    private void BindObjects()
    {
        _mainCamera = Instantiate(_mainCamera);
        _mainDirectionalLight = Instantiate(_mainDirectionalLight);
        _loadingScreen = Instantiate(_loadingScreen);
        _mainEventSystem = Instantiate(_mainEventSystem);
        _mainCanvas = Instantiate(_mainCanvas);
    }

    private async Task InitializeGame() { throw new NotImplementedException(); }
    
    private async Task InitializeObjects()
    {
        throw new NotImplementedException();
    }
    

    private async Task CreateObjects()
    {
        // _background = Instantiate(_background);
        // _player = Instantiate(_player);
        //_enemiesSpawner.CreateEnemies(numberOfEnemies);
    }

    private void PrepareGame()
    {
        // _player.SetActive(true);
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

public class ShowLoadingScreenDisposable : IDisposable
{
    public ShowLoadingScreenDisposable(GameObject loadingScreen) { throw new NotImplementedException(); }
    public void Dispose() { throw new NotImplementedException(); }
}
