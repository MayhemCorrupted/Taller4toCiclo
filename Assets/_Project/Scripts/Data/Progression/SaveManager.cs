using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Configuración de Escenas")]
    [SerializeField] private string _stage1SceneName = "Stage1_Bosque";
    [SerializeField] private string _stage2SceneName = "Stage2_Catedral";
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private GameSaveContainer _currentRun;
    private bool _hasActiveSession;
    private bool _isLoadingFromDisk;
    private bool _isSaving;

    public GameSaveContainer CurrentRun => _currentRun;
    public bool HasActiveSession => _hasActiveSession;
    public bool IsLoadingFromDisk => _isLoadingFromDisk;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BinarySaveService.Initialize();
    }
    private void Update()
    {
        if (_hasActiveSession && _currentRun != null)
        {
            _currentRun.player.playTimeSeconds += Time.deltaTime;
        }
    }
    public async void StartNewGame(string playerNickname)
    {
        try
        {
            BinarySaveService.DeleteRunSave();

            _currentRun = new GameSaveContainer();
            _currentRun.player.nickname = string.IsNullOrWhiteSpace(playerNickname) ? "Faulvalier" : playerNickname.Trim();
            _currentRun.player.currentScene = _stage1SceneName;
            _currentRun.player.isCheckpointActivated = false;
            _currentRun.player.remainingLives = 0;
            _currentRun.player.baseVesania = 0;
            _currentRun.player.totalDeaths = 0;
            _currentRun.player.playTimeSeconds = 0f;
            _currentRun.world.currentState = 0;

            if (StateManager.Instance != null)
            {
                StateManager.Instance.ResetAllProgress();
                StateManager.Instance.ExportStateData(_currentRun.world);
            }

            _hasActiveSession = true;
            _isLoadingFromDisk = false;

            await BinarySaveService.SaveRunAsync(_currentRun);
            await LoadSceneAsyncInternal(_stage1SceneName);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al iniciar nueva partida: {ex.Message}");
        }
    }
    public async void ContinueSavedGame()
    {
        try
        {
            GameSaveContainer loaded = await BinarySaveService.LoadRunAsync();
            if (loaded == null)
            {
                GameLogger.LogWarning("No se encontró partida válida para continuar.");
                return;
            }

            _currentRun = loaded;
            _hasActiveSession = true;
            _isLoadingFromDisk = true;

            string targetScene = string.IsNullOrWhiteSpace(_currentRun.player.currentScene)
                ? _stage1SceneName
                : _currentRun.player.currentScene;

            await LoadSceneAsyncInternal(targetScene);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al continuar partida guardada: {ex.Message}");
        }
    }
    public void ConsumeDiskLoadFlag() => _isLoadingFromDisk = false;
    public void RegisterPartialDeath()
    {
        if (!_hasActiveSession || _currentRun == null) return;
        _currentRun.player.totalDeaths++;
    }
    public async Task AutoSaveRunAsync(string overrideSceneName = null)
    {
        if (!_hasActiveSession || _currentRun == null || _isSaving) return;

        _isSaving = true;
        try
        {
            CollectCurrentGameData(overrideSceneName);
            await BinarySaveService.SaveRunAsync(_currentRun);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error en autoguardado: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }
    public async Task TransitionToNextStageAsync(string nextSceneName, int newStateValue = -1)
    {
        if (!_hasActiveSession || _currentRun == null)
        {
            _currentRun = new GameSaveContainer();
            _hasActiveSession = true;
        }

        CollectCurrentGameData(overrideSceneName: nextSceneName);

        if (newStateValue >= 0)
        {
            _currentRun.world.currentState = newStateValue;
        }

        _isLoadingFromDisk = false;

        _isSaving = true;
        try
        {
            await BinarySaveService.SaveRunAsync(_currentRun);
        }
        finally
        {
            _isSaving = false;
        }

        await LoadSceneAsyncInternal(nextSceneName);
    }
    private void CollectCurrentGameData(string overrideSceneName = null)
    {
        if (_currentRun == null) return;

        _currentRun.player.currentScene = string.IsNullOrWhiteSpace(overrideSceneName)
            ? SceneManager.GetActiveScene().name
            : overrideSceneName;

        if (GameManager.Instance != null)
        {
            _currentRun.player.isCheckpointActivated = GameManager.Instance.IsCheckpointActivated;
            _currentRun.player.remainingLives = GameManager.Instance.CurrentLives;
            _currentRun.player.baseVesania = GameManager.Instance.BaseVesania;
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.ExportStateData(_currentRun.world);
        }
    }
    public async Task HandlePermadeathAsync(Vector3 deathPosition)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string nick = _currentRun != null ? _currentRun.player.nickname : "Faulvalier";
        float time = _currentRun != null ? _currentRun.player.playTimeSeconds : 0f;
        int deaths = _currentRun != null ? _currentRun.player.totalDeaths + 1 : 1;

        _hasActiveSession = false;

        BinarySaveService.DeleteRunSave();

        if (CloudSyncService.Instance != null)
        {
            await CloudSyncService.Instance.SubmitRunResultAsync(new RunSubmissionPayload
            {
                nickname = nick,
                playTime = time,
                won = 0,
                deaths = deaths,
                deathScene = sceneName,
                posX = deathPosition.x,
                posY = deathPosition.y,
                posZ = deathPosition.z
            });
        }

        _currentRun = null;
    }
    public async Task HandleVictoryAsync()
    {
        string nick = _currentRun != null ? _currentRun.player.nickname : "Faulvalier";
        float time = _currentRun != null ? _currentRun.player.playTimeSeconds : 0f;
        int deaths = _currentRun != null ? _currentRun.player.totalDeaths : 0;

        _hasActiveSession = false;

        BinarySaveService.DeleteRunSave();

        if (CloudSyncService.Instance != null)
        {
            await CloudSyncService.Instance.SubmitRunResultAsync(new RunSubmissionPayload
            {
                nickname = nick,
                playTime = time,
                won = 1,
                deaths = deaths,
                deathScene = "",
                posX = 0f,
                posY = 0f,
                posZ = 0f
            });
        }

        _currentRun = null;
    }
    public async void ReturnToMainMenu()
    {
        try
        {
            await LoadSceneAsyncInternal(_mainMenuSceneName);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al regresar al menú principal: {ex.Message}");
        }
    }
    private static async Task LoadSceneAsyncInternal(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            GameLogger.LogError($"No se pudo cargar la escena '{sceneName}'. Verifica que esté añadida en Build Settings.");
            return;
        }

        while (!op.isDone)
        {
            await Task.Yield();
        }
    }
}