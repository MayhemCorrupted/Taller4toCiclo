using System;
using System.Collections.Generic;
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
    private readonly Dictionary<string, ItemData> _runtimeItemCache = new(StringComparer.OrdinalIgnoreCase);
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
    /// Registra un ItemData en la caché persistente de SaveManager para transferirlo entre escenas.
    /// </summary>
    public void RegisterKnownItem(ItemData item)
    {
        if (item == null) return;

        if (!string.IsNullOrWhiteSpace(item.ItemID))
            _runtimeItemCache[item.ItemID.Trim()] = item;

        if (!string.IsNullOrWhiteSpace(item.name))
            _runtimeItemCache[item.name.Trim()] = item;
    }
    /// <summary>
    /// Recupera un ItemData previamente registrado en la sesión por su ID o nombre de asset.
    /// </summary>
    public ItemData ResolveCachedItem(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return null;
        return _runtimeItemCache.TryGetValue(itemID.Trim(), out ItemData cached) ? cached : null;
    }
    /// <summary>
    /// Sincroniza en memoria el inventario actual cada vez que el jugador obtiene, equipa o consume un ítem.
    /// </summary>
    public void SyncInventoryFromManager(InventoryManager inventoryManager)
    {
        if (!_hasActiveSession || _currentRun == null || inventoryManager == null) return;

        _currentRun.inventory ??= new InventorySaveData();
        inventoryManager.ExportInventoryData(_currentRun.inventory);
    }
    /// <summary>
    /// Determina si el jugador debe comenzar tirado en el suelo al cargar la escena actual.
    /// Solo aplica al iniciar en Stage 1 antes de alcanzar su checkpoint.
    /// </summary>
    public bool ShouldPlayerStartLaying()
    {
        if (!_hasActiveSession || _currentRun == null)
            return false;

        string activeScene = SceneManager.GetActiveScene().name;
        bool isStage1 = string.Equals(activeScene, _stage1SceneName, StringComparison.OrdinalIgnoreCase);
        if (!isStage1)
            return false;

        bool checkpointReached = _currentRun.player.isCheckpointActivated ||
                                 (GameManager.Instance != null && GameManager.Instance.IsCheckpointActivated);

        return !checkpointReached;
    }
    /// <summary>
    /// Sincroniza en memoria que el checkpoint de la escena actual fue alcanzado.
    /// </summary>
    public void NotifyCheckpointActivated()
    {
        if (!_hasActiveSession || _currentRun == null) return;
        _currentRun.player.isCheckpointActivated = true;
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

            if (DatabaseManager.Instance != null)
                DatabaseManager.Instance.Characters.CreateCharacter(_currentRun.player.nickname, _currentRun);


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

        DatabaseManager.Instance.Characters.SaveProgress(_currentRun);
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

            DatabaseManager.Instance.Characters.SaveProgress(_currentRun);

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
    /// Guarda el progreso actual (estado, inventario, estadísticas) y carga de forma asíncrona
    /// el siguiente escenario manteniendo el mismo currentState entre ambas escenas.
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

        string currentActiveScene = SceneManager.GetActiveScene().name;
        CollectCurrentGameData(overrideSceneName: nextSceneName);

        if (!string.Equals(currentActiveScene, nextSceneName, StringComparison.OrdinalIgnoreCase))
        {
            _currentRun.player.isCheckpointActivated = false;
        }

        StageSaveData sourceStageData = _currentRun.world.GetStageData(currentActiveScene, _stage1SceneName, _stage2SceneName);
        int carriedState = StateManager.Instance != null
            ? StateManager.Instance.CurrentState
            : sourceStageData.currentState;

        int resolvedState = newStateValue >= 0 ? newStateValue : carriedState;

        StageSaveData destinationStageData = _currentRun.world.GetStageData(nextSceneName, _stage1SceneName, _stage2SceneName);
        destinationStageData.currentState = resolvedState;

        if (_currentRun.world.stage1 != null)
            _currentRun.world.stage1.currentState = resolvedState;
        if (_currentRun.world.stage2 != null)
            _currentRun.world.stage2.currentState = resolvedState;

        _isLoadingFromDisk = false;

        _isSaving = true;
        try
        {
            await BinarySaveService.SaveSlotAsync(_activeSlotIndex, _currentRun);
            DatabaseManager.Instance.Characters.SaveProgress(_currentRun);
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

            if (_currentRun.world.stage1 != null)
                _currentRun.world.stage1.currentState = activeStageData.currentState;
            if (_currentRun.world.stage2 != null)
                _currentRun.world.stage2.currentState = activeStageData.currentState;
        }

        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (inventoryManager != null)
        {
            _currentRun.inventory ??= new InventorySaveData();
            inventoryManager.ExportInventoryData(_currentRun.inventory);
        }
    }
    public async Task HandlePermadeathAsync()
    {
        if (_currentRun != null && DatabaseManager.Instance != null)
        {
            CollectCurrentGameData();
            int finalScore = CalculateFinalScore(false);
            Vector3 deathPos = GameManager.Instance != null && GameManager.Instance.Player != null
                ? GameManager.Instance.Player.transform.position
                : Vector3.zero;

            DatabaseManager.Instance.Characters.FinishRunLose(
                _currentRun.player.characterId,
                finalScore,
                _currentRun.player.playTimeSeconds,
                SceneManager.GetActiveScene().name,
                deathPos
            );
        }

        int slotToDelete = _activeSlotIndex;
        _hasActiveSession = false;
        _activeSlotIndex = -1;
        _currentRun = null;

        if (slotToDelete >= 0)
            BinarySaveService.DeleteSlotSave(slotToDelete);

        await Task.CompletedTask;
    }
    public async Task HandleVictoryAsync()
    {
        if (_currentRun != null && DatabaseManager.Instance != null)
        {
            CollectCurrentGameData();
            int finalScore = CalculateFinalScore(true);

            DatabaseManager.Instance.Characters.FinishRunWin(
                _currentRun.player.characterId,
                finalScore,
                _currentRun.player.playTimeSeconds
            );
        }

        int slotToDelete = _activeSlotIndex;
        _hasActiveSession = false;
        _activeSlotIndex = -1;
        _currentRun = null;

        if (slotToDelete >= 0)
            BinarySaveService.DeleteSlotSave(slotToDelete);

        await Task.CompletedTask;
    }

    public int CalculateFinalScore(bool isVictory)
    {
        int totalKills = 0;
        if (_currentRun != null && _currentRun.world != null)
        {
            if (_currentRun.world.stage1?.stateKillRecords != null)
                foreach (var k in _currentRun.world.stage1.stateKillRecords) totalKills += k.count;

            if (_currentRun.world.stage2?.stateKillRecords != null)
                foreach (var k in _currentRun.world.stage2.stateKillRecords) totalKills += k.count;
        }

        int currentState = StateManager.Instance != null ? StateManager.Instance.CurrentState : 0;
        int livesBonus = _currentRun != null ? _currentRun.player.remainingLives * 250 : 0;
        int victoryBonus = isVictory ? 1000 : 0;

        int rawScore = (totalKills * 150) + (currentState * 200) + livesBonus + victoryBonus;
        return Mathf.Max(100, rawScore);
    }

    public async Task ContinueFromCloudAsync(int slotIndex, string saveJsonFromDb)
    {
        if (string.IsNullOrWhiteSpace(saveJsonFromDb)) return;

        GameSaveContainer cloudRun = JsonUtility.FromJson<GameSaveContainer>(saveJsonFromDb);
        if (cloudRun == null) return;

        cloudRun.player.slotIndex = slotIndex;
        await BinarySaveService.SaveSlotAsync(slotIndex, cloudRun);
        await ContinueSavedGameAsync(slotIndex);
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