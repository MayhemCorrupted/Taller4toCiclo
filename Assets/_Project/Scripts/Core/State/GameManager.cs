using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct VesaniaSpawnGroup
{
    [Tooltip("Nivel de Vesania requerido para activar estos objetos.")]
    public int requiredVesaniaLevel;
    public GameObject[] objectsToActivate;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<int> OnVesaniaChanged;
    public event Action OnGameOver;
    public event Action OnLifeLost;
    public event Action OnPlayerRespawned;

    [Header("Unity Events")]
    public UnityEvent<int> OnVesaniaChangedEvent;
    [Tooltip("Se dispara cuando el jugador muere pero aún le quedan vidas. Conectar aquí FadeUI.StartSequence.")]
    public UnityEvent OnLifeLostEvent;
    [Tooltip("Se dispara justo después de teletransportar y curar al jugador en el checkpoint.")]
    public UnityEvent OnPlayerRespawnedEvent;
    [Tooltip("Se dispara cuando las vidas llegan a 0.")]
    public UnityEvent OnGameOverEvent;

    [Header("Player Settings")]
    [Tooltip("Referencia al GameObject Raíz del jugador. Si es nulo, se buscará automáticamente por Tag 'Player'.")]
    [SerializeField] private GameObject _player;

    [Header("Vesania World Spawns")]
    [Tooltip("Configura aquí los objetos que se revelan con cada nivel de Vesania.")]
    [SerializeField] private VesaniaSpawnGroup[] _vesaniaSpawnGroups;

    [Header("Checkpoint / Respawn Settings")]
    [Tooltip("Transform de la Estatua de Lithoselle (punto de control principal).")]
    [SerializeField] private Transform _statueCheckpointTransform;

    private int _currentLives = 3;
    private int _currentVesania = 1;
    public int CurrentLives => _currentLives;
    public int CurrentVesania => _currentVesania;
    public GameObject Player => _player;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {
                Debug.LogError("GameManager: No se encontró ningún GameObject con el Tag 'Player'.");
            }
        }
    }
    private void Start()
    {
        UpdateVesaniaObjects();
    }
    public void ProcessPlayerDeath()
    {
        if (_currentLives <= 0) return;

        _currentLives--;
        IncreaseVesania();

        if (_currentLives > 0)
        {
            OnLifeLost?.Invoke();
            OnLifeLostEvent?.Invoke();
        }
        else
        {
            TriggerGameOver();
        }
    }
    private void IncreaseVesania()
    {
        if (_currentVesania < 4)
        {
            _currentVesania++;
            OnVesaniaChanged?.Invoke(_currentVesania);
            OnVesaniaChangedEvent?.Invoke(_currentVesania);
            UpdateVesaniaObjects();
        }
    }
    private void TriggerGameOver()
    {
        Debug.Log("Muerte definitiva. Las 3 vidas se han agotado.");
        OnGameOver?.Invoke();
        OnGameOverEvent?.Invoke();
    }
    public void RespawnPlayerAtCheckpoint()
    {
        if (_player == null)
        {
            Debug.LogWarning("GameManager: No hay referencia al GameObject Raíz del jugador para reaparecer.");
            return;
        }

        if (_statueCheckpointTransform != null)
        {
            if (_player.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

            _player.transform.SetPositionAndRotation(_statueCheckpointTransform.position, _statueCheckpointTransform.rotation);

            if (cc != null) cc.enabled = true;
        }

        if (_player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.Heal(playerHealth.MaxHealth);
        }

        PlayerAnimator playerAnimator = _player.GetComponentInChildren<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.RespawnPlayer();
        }

        OnPlayerRespawned?.Invoke();
        OnPlayerRespawnedEvent?.Invoke();
    }
    public void ResetLivesAndVesania()
    {
        _currentLives = 3;
        _currentVesania = 1;
        OnVesaniaChanged?.Invoke(_currentVesania);
        OnVesaniaChangedEvent?.Invoke(_currentVesania);
        UpdateVesaniaObjects();
    }
    private void UpdateVesaniaObjects()
    {
        if (_vesaniaSpawnGroups == null) return;

        foreach (var group in _vesaniaSpawnGroups)
        {
            bool shouldBeActive = _currentVesania >= group.requiredVesaniaLevel;

            foreach (var obj in group.objectsToActivate)
            {
                if (obj != null) obj.SetActive(shouldBeActive);
            }
        }
    }
}