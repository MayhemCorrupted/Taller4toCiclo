using System;
using UnityEngine;

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

    [Header("Vesania World Spawns")]
    [Tooltip("Configura aquí los objetos que se revelan con cada nivel de Vesania.")]
    [SerializeField] private VesaniaSpawnGroup[] _vesaniaSpawnGroups;
    private int _currentLives = 3;
    private int _currentVesania = 1;
    public int CurrentLives => _currentLives;
    public int CurrentVesania => _currentVesania;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        UpdateVesaniaObjects();
    }
    public void HandlePlayerDeath()
    {
        if (_currentLives <= 0) return;

        _currentLives--;
        IncreaseVesania();

        if (_currentLives <= 0)
        {
            TriggerGameOver();
        }
        else
        {
            OnLifeLost?.Invoke();
        }
    }
    private void IncreaseVesania()
    {
        if (_currentVesania < 4)
        {
            _currentVesania++;
            OnVesaniaChanged?.Invoke(_currentVesania);
            UpdateVesaniaObjects();
        }
    }
    private void TriggerGameOver()
    {
        Debug.Log("Muerte definitiva. Las 3 vidas se han agotado.");
        OnGameOver?.Invoke();
    }
    public void ResetLivesAndVesania()
    {
        _currentLives = 3;
        _currentVesania = 1;
        OnVesaniaChanged?.Invoke(_currentVesania);
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