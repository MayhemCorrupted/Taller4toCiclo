using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct VesaniaSpawnGroup
{
    [Tooltip("Nivel de Vesania requerido para aplicar este grupo.")]
    public int requiredVesaniaLevel;

    [Tooltip("Objetos que se activan al alcanzar este nivel de Vesania.")]
    public GameObject[] objectsToActivate;

    [Tooltip("Objetos que se desactivan al alcanzar este nivel de Vesania.")]
    public GameObject[] objectsToDeactivate;
}

public class GameManager : MonoBehaviour
{
    private const int MaxBaseVesania = 3;
    private const int Vesania4Level = 4;

    public static GameManager Instance { get; private set; }

    public event Action<int> OnVesaniaChanged;
    public event Action OnGameOver;
    public event Action OnLifeLost;
    public event Action OnPlayerRespawned;
    public event Action OnCheckpointActivated;

    [Header("Unity Events")]
    public UnityEvent<int> OnVesaniaChangedEvent;
    [Tooltip("Se dispara cuando se activa el checkpoint por primera vez y se habilitan las 3 vidas y la Vesania.")]
    public UnityEvent OnCheckpointActivatedEvent;
    [Tooltip("Se dispara cuando el jugador muere pero aún le quedan vidas tras activar el checkpoint. Conectar aquí FadeUI.StartSequence.")]
    public UnityEvent OnLifeLostEvent;
    [Tooltip("Se dispara justo después de teletransportar y curar al jugador en el punto activo (inicial o checkpoint).")]
    public UnityEvent OnPlayerRespawnedEvent;
    [Tooltip("Se dispara al morir sin checkpoint activo o cuando las 3 vidas se agotan.")]
    public UnityEvent OnGameOverEvent;

    [Header("Player Settings")]
    [Tooltip("Referencia al GameObject Raíz del jugador. Si es nulo, se buscará automáticamente por Tag 'Player'.")]
    [SerializeField] private GameObject _player;

    [Header("Vesania & Lives Settings")]
    [Tooltip("Cantidad de vidas/resurrecciones al activar el checkpoint (3 vidas = progresión de Vesania 0 a 3).")]
    [SerializeField] private int _startingLives = 3;
    [Tooltip("Porcentaje de vida máximo (0.2 = 20%) para activar Vesania 4 estando en Vesania 3.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float _vesania4HealthThreshold = 0.2f;
    [Tooltip("Si es true, purificarse en la Estatua también reinicia la Vesania base a 0.")]
    [SerializeField] private bool _resetVesaniaOnPurify = true;

    [Header("Vesania World Spawns")]
    [Tooltip("Configura aquí los objetos que se activan o desactivan con cada nivel de Vesania.")]
    [SerializeField] private VesaniaSpawnGroup[] _vesaniaSpawnGroups;

    [Header("Checkpoint / Respawn Settings")]
    [Tooltip("Transform de la Estatua de Lithoselle (se usará al activar el checkpoint).")]
    [SerializeField] private Transform _statueCheckpointTransform;

    private PlayerHealth _playerHealth;
    private Vector3 _initialSpawnPosition;
    private Quaternion _initialSpawnRotation;
    private bool _hasCapturedInitialSpawn;

    private bool _isCheckpointActivated;
    private int _currentLives;
    private int _baseVesania;
    private int _currentVesania;
    private bool _isGameOver;

    private readonly Dictionary<GameObject, bool> _resolvedObjectStates = new();
    private readonly List<VesaniaSpawnGroup> _sortedGroups = new();

    public bool IsCheckpointActivated => _isCheckpointActivated;
    public int CurrentLives => _currentLives;
    public int BaseVesania => _baseVesania;
    public int CurrentVesania => _currentVesania;
    public bool IsVesania4Active => _isCheckpointActivated && _currentVesania == Vesania4Level;
    public GameObject Player => _player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _isCheckpointActivated = false;
        _currentLives = 0;
        _baseVesania = 0;
        _currentVesania = 0;

        ResolvePlayerReferences();
        CaptureInitialSpawnPoint();
    }

    private void OnEnable()
    {
        SubscribePlayerHealth();
    }

    private void OnDisable()
    {
        UnsubscribePlayerHealth();
    }

    private void Start()
    {
        ResolvePlayerReferences();
        if (!_hasCapturedInitialSpawn)
            CaptureInitialSpawnPoint();

        SubscribePlayerHealth();

        SyncFromSaveManager();

        EvaluateVesaniaState(forceNotify: true);
    }

    private void SyncFromSaveManager()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasActiveSession) return;

        PlayerSaveData pData = SaveManager.Instance.CurrentRun.player;
        _isCheckpointActivated = pData.isCheckpointActivated;
        _currentLives = pData.remainingLives;
        _baseVesania = Mathf.Clamp(pData.baseVesania, 0, MaxBaseVesania);

        if (SaveManager.Instance.IsLoadingFromDisk && _isCheckpointActivated && _statueCheckpointTransform != null)
        {
            TeleportPlayerTo(_statueCheckpointTransform.position, _statueCheckpointTransform.rotation);
            SaveManager.Instance.ConsumeDiskLoadFlag();
        }
    }

    private void ResolvePlayerReferences()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {
                GameLogger.LogError("GameManager: No se encontró ningún GameObject con el Tag 'Player'.");
                return;
            }
        }

        if (_playerHealth == null)
        {
            _playerHealth = _player.GetComponent<PlayerHealth>();
            if (_playerHealth == null)
                _playerHealth = _player.GetComponentInChildren<PlayerHealth>();
        }
    }

    private void CaptureInitialSpawnPoint()
    {
        if (_player == null) return;

        _initialSpawnPosition = _player.transform.position;
        _initialSpawnRotation = _player.transform.rotation;
        _hasCapturedInitialSpawn = true;
    }

    private void SubscribePlayerHealth()
    {
        if (_playerHealth == null) return;

        _playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
        _playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
    }

    private void UnsubscribePlayerHealth()
    {
        if (_playerHealth == null) return;

        _playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
    }

    private void HandlePlayerHealthChanged()
    {
        EvaluateVesaniaState();
    }

    public void ActivateCheckpoint()
    {
        if (_isCheckpointActivated) return;

        _isCheckpointActivated = true;
        _currentLives = _startingLives;
        _baseVesania = 0;

        EvaluateVesaniaState(forceNotify: true);

        OnCheckpointActivated?.Invoke();
        OnCheckpointActivatedEvent?.Invoke();

        if (SaveManager.Instance != null)
        {
            _ = SaveManager.Instance.AutoSaveRunAsync();
        }
    }

    public void ActivateCheckpoint(Transform newCheckpointTransform)
    {
        if (newCheckpointTransform != null)
        {
            _statueCheckpointTransform = newCheckpointTransform;
        }

        ActivateCheckpoint();
    }

    /// <summary>
    /// Conectar a la opción "Curar / Purificar" en el diálogo de Lithoselle.
    /// Restaura las 3 vidas, cura HP al máximo y guarda el progreso.
    /// </summary>
    public void PurifyAtStatue()
    {
        if (!_isCheckpointActivated)
        {
            ActivateCheckpoint();
        }
        else
        {
            _currentLives = _startingLives;
            if (_resetVesaniaOnPurify)
            {
                _baseVesania = 0;
            }
            EvaluateVesaniaState(forceNotify: true);
        }

        if (_playerHealth == null)
            ResolvePlayerReferences();

        if (_playerHealth != null)
        {
            _playerHealth.Heal(_playerHealth.MaxHealth);
        }

        if (SaveManager.Instance != null)
        {
            _ = SaveManager.Instance.AutoSaveRunAsync();
        }
    }

    public void ProcessPlayerDeath()
    {
        if (_isGameOver) return;

        if (!_isCheckpointActivated)
        {
            _currentLives = 0;
            _isGameOver = true;
            TriggerGameOver();
            return;
        }

        if (_currentLives > 0)
        {
            _currentLives--;
            _baseVesania = Mathf.Clamp(_baseVesania + 1, 0, MaxBaseVesania);

            EvaluateVesaniaState();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterPartialDeath();
                _ = SaveManager.Instance.AutoSaveRunAsync();
            }

            OnLifeLost?.Invoke();
            OnLifeLostEvent?.Invoke();
        }
        else
        {
            _isGameOver = true;
            TriggerGameOver();
        }
    }

    private void EvaluateVesaniaState(bool forceNotify = false)
    {
        int targetVesania = 0;

        if (_isCheckpointActivated)
        {
            targetVesania = _baseVesania;

            if (_baseVesania >= MaxBaseVesania && IsPlayerInLowHealthThreshold())
            {
                targetVesania = Vesania4Level;
            }
        }

        if (forceNotify || targetVesania != _currentVesania)
        {
            _currentVesania = targetVesania;
            OnVesaniaChanged?.Invoke(_currentVesania);
            OnVesaniaChangedEvent?.Invoke(_currentVesania);
            UpdateVesaniaObjects();
        }
    }

    private bool IsPlayerInLowHealthThreshold()
    {
        if (_playerHealth == null)
            ResolvePlayerReferences();

        if (_playerHealth == null || _playerHealth.MaxHealth <= 0f)
            return false;

        if (_playerHealth.CurrentHealth <= 0f)
            return false;

        float healthRatio = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
        return healthRatio <= _vesania4HealthThreshold;
    }

    private void TriggerGameOver()
    {
        GameLogger.LogInfo(_isCheckpointActivated
            ? "Muerte definitiva. Las 3 vidas se han agotado."
            : "Muerte antes de activar el checkpoint. Reinicio requerido.");

        Vector3 deathPosition = _player != null ? _player.transform.position : Vector3.zero;

        if (SaveManager.Instance != null)
        {
            _ = SaveManager.Instance.HandlePermadeathAsync(deathPosition);
        }

        OnGameOver?.Invoke();
        OnGameOverEvent?.Invoke();
    }

    public void RespawnPlayerAtCheckpoint()
    {
        if (_player == null)
        {
            GameLogger.LogWarning("GameManager: No hay referencia al GameObject Raíz del jugador para reaparecer.");
            return;
        }

        Vector3 targetPos = _initialSpawnPosition;
        Quaternion targetRot = _initialSpawnRotation;

        if (_isCheckpointActivated && _statueCheckpointTransform != null)
        {
            _statueCheckpointTransform.GetPositionAndRotation(out targetPos, out targetRot);
        }

        TeleportPlayerTo(targetPos, targetRot);

        if (_playerHealth == null)
            ResolvePlayerReferences();

        if (_playerHealth != null)
        {
            _playerHealth.Heal(_playerHealth.MaxHealth);
        }

        PlayerAnimator playerAnimator = _player.GetComponentInChildren<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.RespawnPlayer();
        }

        EvaluateVesaniaState();

        OnPlayerRespawned?.Invoke();
        OnPlayerRespawnedEvent?.Invoke();
    }

    private void TeleportPlayerTo(Vector3 targetPos, Quaternion targetRot)
    {
        if (_player == null) return;

        if (_player.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

        _player.transform.SetPositionAndRotation(targetPos, targetRot);
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;
    }

    public void ResetLivesAndVesania()
    {
        _isGameOver = false;
        _isCheckpointActivated = false;
        _currentLives = 0;
        _baseVesania = 0;

        EvaluateVesaniaState(forceNotify: true);
    }

    private void UpdateVesaniaObjects()
    {
        if (_vesaniaSpawnGroups == null || _vesaniaSpawnGroups.Length == 0) return;

        _resolvedObjectStates.Clear();
        _sortedGroups.Clear();

        for (int i = 0; i < _vesaniaSpawnGroups.Length; i++)
        {
            _sortedGroups.Add(_vesaniaSpawnGroups[i]);
        }

        _sortedGroups.Sort((a, b) => a.requiredVesaniaLevel.CompareTo(b.requiredVesaniaLevel));

        for (int i = 0; i < _sortedGroups.Count; i++)
        {
            var group = _sortedGroups[i];

            if (group.objectsToDeactivate != null)
            {
                for (int j = 0; j < group.objectsToDeactivate.Length; j++)
                {
                    GameObject obj = group.objectsToDeactivate[j];
                    if (obj != null && !_resolvedObjectStates.ContainsKey(obj))
                        _resolvedObjectStates[obj] = true;
                }
            }

            if (group.objectsToActivate != null)
            {
                for (int j = 0; j < group.objectsToActivate.Length; j++)
                {
                    GameObject obj = group.objectsToActivate[j];
                    if (obj != null)
                        _resolvedObjectStates[obj] = false;
                }
            }
        }

        for (int i = 0; i < _sortedGroups.Count; i++)
        {
            var group = _sortedGroups[i];
            if (_currentVesania < group.requiredVesaniaLevel) continue;

            if (group.objectsToActivate != null)
            {
                for (int j = 0; j < group.objectsToActivate.Length; j++)
                {
                    GameObject obj = group.objectsToActivate[j];
                    if (obj != null)
                        _resolvedObjectStates[obj] = true;
                }
            }

            if (group.objectsToDeactivate != null)
            {
                for (int j = 0; j < group.objectsToDeactivate.Length; j++)
                {
                    GameObject obj = group.objectsToDeactivate[j];
                    if (obj != null)
                        _resolvedObjectStates[obj] = false;
                }
            }
        }

        foreach (var kvp in _resolvedObjectStates)
        {
            if (kvp.Key != null && kvp.Key.activeSelf != kvp.Value)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }
    }
}