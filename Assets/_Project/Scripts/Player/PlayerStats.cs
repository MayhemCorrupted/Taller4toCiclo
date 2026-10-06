using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsUpdated;

    [Range(1,10)] public int _lifeLevel = 1;
    [Range(1,10)] public int _staminaLevel = 1;
    [Range(1,10)] public int _strengthLevel = 1;
    [Range(1,10)] public int _agilityLevel = 1;

    [Range(1,4)] public int _vesaniaLevel = 1;
    public float _armorDefense = 20;

    public float MaxHealth { get; private set; }
    public float MaxStamina { get; private set; }
    public float DamageMultiplier { get; private set; }
    public float SpeedMultiplier { get; private set; }
    public int Iframes { get; private set; }
    public float Defense { get; private set; }
    public float VesaniaDamageBonus { get; private set; }

    private void Awake()
    {
        RecalculateStats();
    }
    public void RecalculateStats()
    {
        MaxHealth = 80 + (12 * _lifeLevel);
        MaxStamina = 60 + (8 * _staminaLevel);
        float strengthBonus = 0;
        if (_strengthLevel <=  6)
        {
            strengthBonus = _strengthLevel * 0.05f;
        }
        else
        {
            strengthBonus = (6 * 0.05f) + ((_strengthLevel - 6) * 0.03f);
        }

        DamageMultiplier = 1 + strengthBonus;

        SpeedMultiplier = 1 + (_agilityLevel * 0.03f);

        Iframes = Mathf.CeilToInt(12 + (_agilityLevel - 1) / 2);

        Defense = 10f + _armorDefense;
        
        VesaniaDamageBonus = 0;
        
        if (_vesaniaLevel == 2)
        {
            VesaniaDamageBonus = 0.10f;
        }
        else if (_vesaniaLevel == 3)
        {
            VesaniaDamageBonus = 0.20f;
        }
        else if (_vesaniaLevel == 4)
        {
            VesaniaDamageBonus = 0.30f;
        }

        OnStatsUpdated?.Invoke();
    }
}
