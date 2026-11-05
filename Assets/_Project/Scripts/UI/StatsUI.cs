using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour, IPanel
{
    PlayerStats _stats;
    PlayerHealth _health;
    [SerializeField] private GameObject _statsContainer;
    [SerializeField] private TextMeshProUGUI _availablePointsText;
    [SerializeField] private GameObject _renderPreviewCamera;

    [Header("Orden: 0=Vida, 1=Fuerza, 2=Stamina, 3=Agilidad")]
    [SerializeField] private TextMeshProUGUI[] _statsText;
    [SerializeField] private Button[] _increaseStatButton; 
    [SerializeField] private Button[] _decreaseStatButton;
    [SerializeField] private Button _confirmButton;

    private int _availablePoints = 0;
    private readonly int[] _baseStats = new int[4];
    private readonly int[] _tempStats = new int[4];
    private void Awake()
    {
        _stats = FindAnyObjectByType<PlayerStats>();
        _health = FindAnyObjectByType<PlayerHealth>();

        for (int i = 0; i < 4; i++)
        {
            int index = i; 

            if (_increaseStatButton.Length > i && _increaseStatButton[index] != null)
                _increaseStatButton[index].onClick.AddListener(() => IncreaseStat(index));

            if (_decreaseStatButton.Length > i && _decreaseStatButton[index] != null)
                _decreaseStatButton[index].onClick.AddListener(() => DecreaseStat(index));
        }

        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(false);

        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(ConfirmStats);
    }
    private void Start()
    {
        GrantStatPoints(6);
        OpenPanel();    
    }
    public void GrantStatPoints(int points)
    {
        _availablePoints += points;
        OpenPanel();
    }
    public void OpenPanel()
    {
        if (_statsContainer != null) _statsContainer.SetActive(true);
        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(true);
        UIManager.Instance.SetGameState(GameState.Stadistics);

        LoadCurrentStats();
        UpdateUI();
    }
    public void ClosePanel()
    {
        if (_statsContainer != null) _statsContainer.SetActive(false);
        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(false);
        UIManager.Instance.SetGameState(GameState.Gameplay);
    }
    private void LoadCurrentStats()
    {
        _baseStats[0] = _stats._lifeLevel;
        _baseStats[1] = _stats._strengthLevel;
        _baseStats[2] = _stats._staminaLevel;
        _baseStats[3] = _stats._agilityLevel;

        Array.Copy(_baseStats, _tempStats, 4);
    }
    private void IncreaseStat(int index)
    {
        if (_availablePoints > 0 && _tempStats[index] < 10)
        {
            _tempStats[index]++;
            _availablePoints--;
            UpdateUI();
        }
    }
    private void DecreaseStat(int index)
    {
        if (_tempStats[index] > _baseStats[index])
        {
            _tempStats[index]--;
            _availablePoints++;
            UpdateUI();
        }
    }
    private void ConfirmStats()
    {
        _stats._lifeLevel = _tempStats[0];
        _stats._strengthLevel = _tempStats[1];
        _stats._staminaLevel = _tempStats[2];
        _stats._agilityLevel = _tempStats[3];

        _stats.RecalculateStats();
        _health.SetHealth();
        ClosePanel();
    }
    private void UpdateUI()
    {
        if (_availablePointsText != null)
            _availablePointsText.text = $"{_availablePoints}";

        for (int i = 0; i < 4; i++)
        {
            if (_statsText.Length > i && _statsText[i] != null)
                _statsText[i].text = _tempStats[i].ToString();

            if (_increaseStatButton.Length > i && _increaseStatButton[i] != null)
                _increaseStatButton[i].interactable = (_availablePoints > 0 && _tempStats[i] < 10);

            if (_decreaseStatButton.Length > i && _decreaseStatButton[i] != null)
                _decreaseStatButton[i].interactable = (_tempStats[i] > _baseStats[i]);
        }

        if (_confirmButton != null)
            _confirmButton.interactable = (_availablePoints == 0);
    }
}
