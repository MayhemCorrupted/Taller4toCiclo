using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Nombres de Escenas (Build Settings)")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _introSetupSceneName = "Intro_CharacterSetup";
    [SerializeField] private string _stage1SceneName = "Stage1_Bosque";
    [SerializeField] private string _stage2SceneName = "Stage2_Catedral";

    [Header("Transición Fade (Opcional - Persistente)")]
    [SerializeField] private FadeUI _transitionFadeUI;
    [SerializeField] private float _sceneFadeDuration = 0.45f;

    [Header("Conteo de Tiempo Jugado")]
    [Tooltip("Si está activo, el reloj solo corre mientras la ventana del juego tiene el foco.")]
    [SerializeField] private bool _onlyCountPlayTimeWhenFocused = true;

    private GameSaveContainer _currentRun;
    private int _activeSlotIndex = -1;
    private bool _hasActiveSession;
    private bool _isLoadingFromDisk;
    private bool _isSaving;
    private bool _isTransitioningScene;

    public GameSaveContainer CurrentRun => _currentRun;
    public int ActiveSlotIndex => _activeSlotIndex;
    public bool HasActiveSession => _hasActiveSession;
    public bool IsLoadingFromDisk => _isLoadingFromDisk;
    public bool IsTransitioningScene => _isTransitioningScene;
    public string Stage1SceneName => _stage1SceneName;
    public string Stage2SceneName => _stage2SceneName;
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

        if (_transitionFadeUI == null)
            _transitionFadeUI = GetComponentInChildren<FadeUI>(true);
    }
    private void Update()
    {
        if (!_hasActiveSession || _currentRun == null) return;
        if (_onlyCountPlayTimeWhenFocused && !Application.isFocused) return;

        _currentRun.player.playTimeSeconds += Time.unscaledDeltaTime;
    }
    /// <summary>
    /// Inicia una nueva partida en el slot elegido, guarda los datos iniciales
    /// y carga de forma asíncrona la escena intermedia de Estadísticas + Diálogo.
    /// </summary>
    public async Task StartNewGameAsync(int slotIndex, string playerNickname)
    {
        if (_isTransitioningScene || slotIndex < 0 || slotIndex >= BinarySaveService.MAX_SLOTS)
            return;

        try
        {
            BinarySaveService.DeleteSlotSave(slotIndex);

            _activeSlotIndex = slotIndex;
            _currentRun = new GameSaveContainer();
            _currentRun.player.slotIndex = slotIndex;
            _currentRun.player.nickname = string.IsNullOrWhiteSpace(playerNickname)
                ? "Faulvalier"
                : playerNickname.Trim();
            _currentRun.player.currentScene = _stage1SceneName;
            _currentRun.player.isCheckpointActivated = false;
            _currentRun.player.remainingLives = 0;
            _currentRun.player.baseVesania = 0;
            _currentRun.player.totalDeaths = 0;
            _currentRun.player.playTimeSeconds = 0f;

            _currentRun.player.statVida = 1;
            _currentRun.player.statStamina = 1;
            _currentRun.player.statFuerza = 1;
            _currentRun.player.statAgilidad = 1;

            _currentRun.world.stage1 = new StageSaveData { stageSceneName = _stage1SceneName, currentState = 0 };
            _currentRun.world.stage2 = new StageSaveData { stageSceneName = _stage2SceneName, currentState = 0 };

            _hasActiveSession = true;
            _isLoadingFromDisk = false;

            await BinarySaveService.SaveSlotAsync(_activeSlotIndex, _currentRun);
            await LoadSceneWithFadeAsync(_introSetupSceneName);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al iniciar nueva partida en Slot {slotIndex + 1}: {ex.Message}");
        }
    }
    /// <summary>
    /// Guarda las estadísticas iniciales elegidas por el jugador antes de iniciar el diálogo de introducción.
    /// </summary>
    public async Task ConfirmInitialStatsAsync(int vidaLevel, int staminaLevel, int fuerzaLevel, int agilidadLevel)
    {
        if (!_hasActiveSession || _currentRun == null) return;

        _currentRun.player.statVida = Mathf.Clamp(vidaLevel, 1, 10);
        _currentRun.player.statStamina = Mathf.Clamp(staminaLevel, 1, 10);
        _currentRun.player.statFuerza = Mathf.Clamp(fuerzaLevel, 1, 10);
        _currentRun.player.statAgilidad = Mathf.Clamp(agilidadLevel, 1, 10);

        await BinarySaveService.SaveSlotAsync(_activeSlotIndex, _currentRun);
    }
    /// <summary>
    /// Carga la partida guardada del slot seleccionado y transiciona directamente a su escena guardada.
    /// </summary>
    public async Task ContinueSavedGameAsync(int slotIndex)
    {
        if (_isTransitioningScene || slotIndex < 0 || slotIndex >= BinarySaveService.MAX_SLOTS)
            return;

        try
        {
            GameSaveContainer loaded = await BinarySaveService.LoadSlotAsync(slotIndex);
            if (loaded == null)
            {
                GameLogger.LogWarning($"No se encontró partida válida en Slot {slotIndex + 1}.");
                return;
            }

            _activeSlotIndex = slotIndex;
            _currentRun = loaded;
            _hasActiveSession = true;
            _isLoadingFromDisk = true;

            string targetScene = string.IsNullOrWhiteSpace(_currentRun.player.currentScene)
                ? _stage1SceneName
                : _currentRun.player.currentScene;

            await LoadSceneWithFadeAsync(targetScene);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al cargar partida del Slot {slotIndex + 1}: {ex.Message}");
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
        if (!_hasActiveSession || _currentRun == null || _isSaving || _activeSlotIndex < 0)
            return;

        _isSaving = true;
        try
        {
            CollectCurrentGameData(overrideSceneName);
            await BinarySaveService.SaveSlotAsync(_activeSlotIndex, _currentRun);
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
    /// <summary>
    /// Guarda el progreso actual y carga de forma asíncrona el siguiente escenario (Stage 1 o Stage 2).
    /// </summary>
    public async Task TransitionToNextStageAsync(string nextSceneName, int newStateValue = -1)
    {
        if (_isTransitioningScene) return;

        if (!_hasActiveSession || _currentRun == null)
        {
            _currentRun = new GameSaveContainer();
            _hasActiveSession = true;
            if (_activeSlotIndex < 0) _activeSlotIndex = 0;
        }

        CollectCurrentGameData(overrideSceneName: nextSceneName);

        if (newStateValue >= 0)
        {
            StageSaveData destinationStageData = _currentRun.world.GetStageData(nextSceneName, _stage1SceneName, _stage2SceneName);
            destinationStageData.currentState = newStateValue;
        }

        _isLoadingFromDisk = false;

        _isSaving = true;
        try
        {
            await BinarySaveService.SaveSlotAsync(_activeSlotIndex, _currentRun);
        }
        finally
        {
            _isSaving = false;
        }

        await LoadSceneWithFadeAsync(nextSceneName);
    }
    public StageSaveData GetActiveStageSaveData()
    {
        if (_currentRun == null) return null;
        string activeScene = SceneManager.GetActiveScene().name;
        return _currentRun.world.GetStageData(activeScene, _stage1SceneName, _stage2SceneName);
    }
    private void CollectCurrentGameData(string overrideSceneName = null)
    {
        if (_currentRun == null) return;

        string currentActiveScene = SceneManager.GetActiveScene().name;

        _currentRun.player.currentScene = string.IsNullOrWhiteSpace(overrideSceneName)
            ? currentActiveScene
            : overrideSceneName;

        if (GameManager.Instance != null)
        {
            _currentRun.player.isCheckpointActivated = GameManager.Instance.IsCheckpointActivated;
            _currentRun.player.remainingLives = GameManager.Instance.CurrentLives;
            _currentRun.player.baseVesania = GameManager.Instance.BaseVesania;
        }

        if (StateManager.Instance != null)
        {
            StageSaveData activeStageData = _currentRun.world.GetStageData(currentActiveScene, _stage1SceneName, _stage2SceneName);
            StateManager.Instance.ExportStateData(activeStageData);
        }

        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.ExportInventoryData(_currentRun.inventory);
        }
    }
    public async Task HandlePermadeathAsync()
    {
        int slotToDelete = _activeSlotIndex;
        _hasActiveSession = false;
        _activeSlotIndex = -1;
        _currentRun = null;

        if (slotToDelete >= 0)
        {
            BinarySaveService.DeleteSlotSave(slotToDelete);
        }

        await Task.CompletedTask;
    }
    public async Task HandleVictoryAsync()
    {
        int slotToDelete = _activeSlotIndex;
        _hasActiveSession = false;
        _activeSlotIndex = -1;
        _currentRun = null;

        if (slotToDelete >= 0)
        {
            BinarySaveService.DeleteSlotSave(slotToDelete);
        }

        await Task.CompletedTask;
    }
    public async void ReturnToMainMenu()
    {
        if (_isTransitioningScene) return;

        try
        {
            if (_hasActiveSession && _currentRun != null)
            {
                await AutoSaveRunAsync();
            }

            _hasActiveSession = false;
            Time.timeScale = 1f;
            await LoadSceneWithFadeAsync(_mainMenuSceneName);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al regresar al menú principal: {ex.Message}");
        }
    }
    private async Task LoadSceneWithFadeAsync(string sceneName)
    {
        if (_isTransitioningScene) return;
        _isTransitioningScene = true;

        try
        {
            FadeUI activeFade = ResolveFadeUI();
            if (activeFade != null)
            {
                await activeFade.FadeInAsync(_sceneFadeDuration);
            }

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

            await Task.Yield();

            activeFade = ResolveFadeUI();
            if (activeFade != null)
            {
                await activeFade.FadeOutAsync(_sceneFadeDuration);
            }
        }
        finally
        {
            _isTransitioningScene = false;
        }
    }
    private FadeUI ResolveFadeUI()
    {
        if (_transitionFadeUI != null) return _transitionFadeUI;
        return FindAnyObjectByType<FadeUI>(FindObjectsInactive.Include);
    }
}