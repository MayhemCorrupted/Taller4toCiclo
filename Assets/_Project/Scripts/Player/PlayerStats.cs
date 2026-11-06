using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsUpdated;

    [Range(1, 10)] public int _lifeLevel = 1;
    [Range(1, 10)] public int _staminaLevel = 1;
    [Range(1, 10)] public int _strengthLevel = 1;
    [Range(1, 10)] public int _agilityLevel = 1;

    public float _armorDefense = 0;
    public float MaxHealth { get; private set; }
    public float MaxStamina { get; private set; }
    public float DamageMultiplier { get; private set; }
    public float SpeedMultiplier { get; private set; }
    public int Iframes { get; private set; }
    public float Defense { get; private set; }
    public float VesaniaDamageBonus { get; private set; }
    public float ConsumableDamageBonus { get; private set; }
    private void Awake()
    {
        RecalculateStats();
    }
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVesaniaChanged += HandleVesaniaChanged;

            UpdateVesaniaBonus(GameManager.Instance.CurrentVesania);
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVesaniaChanged -= HandleVesaniaChanged;
        }
    }
    private void HandleVesaniaChanged(int newVesaniaLevel)
    {
        UpdateVesaniaBonus(newVesaniaLevel);
    }
    private void UpdateVesaniaBonus(int level)
    {
        VesaniaDamageBonus = 0f;

        if (level == 2) VesaniaDamageBonus = 0.10f;
        else if (level == 3) VesaniaDamageBonus = 0.20f;
        else if (level == 4) VesaniaDamageBonus = 0.30f;

        RecalculateStats();
    }
    public void ApplyTemporaryDamageBoost(float amount)
    {
        ConsumableDamageBonus = amount;
        RecalculateStats();
    }
    public void RecalculateStats()
    {
        MaxHealth = 80 + (12 * _lifeLevel);
        MaxStamina = 60 + (8 * _staminaLevel);
        float strengthBonus;
        if (_strengthLevel <= 6) strengthBonus = _strengthLevel * 0.05f;
        else strengthBonus = (6 * 0.05f) + ((_strengthLevel - 6) * 0.03f);

        DamageMultiplier = 1 + strengthBonus + ConsumableDamageBonus + VesaniaDamageBonus;

        SpeedMultiplier = 1 + (_agilityLevel * 0.03f); 

        Iframes = Mathf.CeilToInt(12 + (_agilityLevel - 1) / 2);

        Defense = 10f + _armorDefense;

        OnStatsUpdated?.Invoke();
    }
}