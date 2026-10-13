using UnityEngine;
public enum ConsumableEffectType { Heal, StatBoost, DamageBoost }

[CreateAssetMenu(fileName = "ConsumableItem", menuName = "Rotbound/Items/Consumable")]
public class ConsumableItem : ItemData
{
    public ConsumableEffectType EffectType;
    public float EffectValue;
    public float EffectDuration;
}
