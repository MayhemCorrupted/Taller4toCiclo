using UnityEngine;
using System;
using System.Collections;
using System.Linq;
[Serializable] public class InventorySlot
{
    public ItemData Item;
    public int Quantity;
    public bool IsEmpty => Item == null || Quantity <= 0;
    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}
public class InventoryManager : MonoBehaviour
{
    public event Action<ArmorItem> OnArmorEquipped;
    public event Action<ConsumableItem, int> OnConsumableEquipped;
    public event Action<InventorySlot[]> OnInventoryUpdated;
    public event Action<InventorySlot[]> OnArmorInventoryUpdated;
    public InventorySlot[] GeneralSlots => _generalSlots;
    public InventorySlot[] ArmorInventorySlots => _armorInventorySlots;
    public ArmorItem EquippedArmor => _equippedArmor;
    public ConsumableItem EquippedConsumable => _equippedConsumable;
    public int ConsumableCount => _consumableCount;
    private ArmorItem _equippedArmor;
    private ConsumableItem _equippedConsumable;
    private int _consumableCount = 0;
    [SerializeField] private InventorySlot[] _generalSlots = new InventorySlot[6];
    [SerializeField] private InventorySlot[] _armorInventorySlots = new InventorySlot[3];

    private PlayerInputs _playerInputs;
    private PlayerStats _playerStats;
    private PlayerHealth _playerHealth;

    private Coroutine _activeBuffCoroutine;
    private Coroutine _activeHealCoroutine;
    private void Awake()
    {
        _playerInputs = FindAnyObjectByType<PlayerInputs>();
        _playerStats = FindAnyObjectByType<PlayerStats>();
        _playerHealth = FindAnyObjectByType<PlayerHealth>();

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            _generalSlots[i] = new InventorySlot();
        }
        for (int i = 0; i < _armorInventorySlots.Length; i++)
        {
            _armorInventorySlots[i] = new InventorySlot();
        }
    }
    private void OnEnable()
    {
      //  _playerInputs.OnUseItem += UseEquippedConsumable;
    }
    private void OnDisable()
    {
       // _playerInputs.OnUseItem -= UseEquippedConsumable;
    }
    public void AddItemToInventory(ItemData item, int amount)
    {
        if (item is ArmorItem)
        {
            AddToSpecificArray(_armorInventorySlots, item, amount, OnArmorInventoryUpdated);
        }
        else if (item is ConsumableItem consumable)
        {
            if (_equippedConsumable != null && _equippedConsumable.ItemID == consumable.ItemID)
            {
                _consumableCount += amount;
                OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);
            }
            else
            {
                AddToSpecificArray(_generalSlots, item, amount, OnInventoryUpdated);
            }
        }
        else
        {
            AddToSpecificArray(_generalSlots, item, amount, OnInventoryUpdated);
        }
    }
    private void AddToSpecificArray(InventorySlot[] slotArray, ItemData item, int amount, Action<InventorySlot[]> updateEvent)
    {
        InventorySlot existingSlot = slotArray.FirstOrDefault(s => !s.IsEmpty && s.Item.ItemID == item.ItemID);
        if (existingSlot != null)
        {
            existingSlot.Quantity += amount;
            updateEvent?.Invoke(slotArray);
            return;
        }

        InventorySlot emptySlot = slotArray.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null)
        {
            emptySlot.Item = item;
            emptySlot.Quantity = amount;
            updateEvent?.Invoke(slotArray);
        }
    }
    public void EquipArmorFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _armorInventorySlots.Length) return;

        InventorySlot slot = _armorInventorySlots[slotIndex];
        if (slot.IsEmpty || slot.Item is not ArmorItem armor) return;

        ArmorItem previousArmor = _equippedArmor;

        _equippedArmor = armor;
        _playerStats._armorDefense = armor.DefenseBonus;
        _playerStats.RecalculateStats();

        OnArmorEquipped?.Invoke(_equippedArmor);

        if (previousArmor != null)
        {
            slot.Item = previousArmor;
            slot.Quantity = 1;
        }
        else slot.Clear();

        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
    }
    public void EquipConsumableFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _generalSlots.Length) return;

        InventorySlot slot = _generalSlots[slotIndex];
        if (slot.IsEmpty || slot.Item is not ConsumableItem consumable) return;

        ConsumableItem previousConsumable = _equippedConsumable;
        int previousCount = _consumableCount;

        _equippedConsumable = consumable;
        _consumableCount = slot.Quantity;

        OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);

        if (previousConsumable != null)
        {
            slot.Item = previousConsumable;
            slot.Quantity = previousCount;
        }
        else
        {
            slot.Clear();
        }
        OnInventoryUpdated?.Invoke(_generalSlots);
    }
    public void SwapGeneralSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _generalSlots.Length || indexB < 0 || indexB >= _generalSlots.Length) return;

        (_generalSlots[indexB], _generalSlots[indexA]) = (_generalSlots[indexA], _generalSlots[indexB]);
        OnInventoryUpdated?.Invoke(_generalSlots);
    }
    public void SwapArmorInventorySlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _armorInventorySlots.Length || indexB < 0 || indexB >= _armorInventorySlots.Length) return;
        (_armorInventorySlots[indexB], _armorInventorySlots[indexA]) = (_armorInventorySlots[indexA], _armorInventorySlots[indexB]);
        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
    }
    public void UnequipArmor(int targetSlotIndex)
    {
        if (_equippedArmor == null || targetSlotIndex < 0 || targetSlotIndex >= _generalSlots.Length) return;
        if (!_generalSlots[targetSlotIndex].IsEmpty) return;

        _armorInventorySlots[targetSlotIndex].Item = _equippedArmor;
        _armorInventorySlots[targetSlotIndex].Quantity = 1;

        _equippedArmor = null;
        _playerStats._armorDefense = 0;
        _playerStats.RecalculateStats();

        OnArmorEquipped?.Invoke(null);
        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
    }
    public void UnequipConsumable(int targetSlotIndex)
    {
        if (_equippedConsumable == null || targetSlotIndex < 0 || targetSlotIndex >= _generalSlots.Length) return;
        if (!_generalSlots[targetSlotIndex].IsEmpty) return;

        _generalSlots[targetSlotIndex].Item = _equippedConsumable;
        _generalSlots[targetSlotIndex].Quantity = _consumableCount;

        _equippedConsumable = null;
        _consumableCount = 0;

        OnConsumableEquipped?.Invoke(null, 0);
        OnInventoryUpdated?.Invoke(_generalSlots);
    }
    private void UseEquippedConsumable()
    {
        if (_equippedConsumable == null || _consumableCount <= 0) return;

        bool wasUsed = false;

        switch (_equippedConsumable.EffectType)
        {
            case ConsumableEffectType.Heal:
                if (_playerHealth.CurrentHealth < _playerHealth.MaxHealth)
                {
                    if (_activeHealCoroutine != null) StopCoroutine(_activeHealCoroutine);
                    _activeHealCoroutine = StartCoroutine(GradualHealRoutine(_equippedConsumable.EffectValue, 5f));
                    wasUsed = true;
                }
                break;

            case ConsumableEffectType.DamageBoost:
                if (_activeBuffCoroutine != null) StopCoroutine(_activeBuffCoroutine);
                _activeBuffCoroutine = StartCoroutine(DamageBoostRoutine(_equippedConsumable.EffectValue, 25));
                wasUsed = true;
                break;

            case ConsumableEffectType.StatBoost:
                wasUsed = true;
                break;
        }

        if (wasUsed)
        {
            _consumableCount--;
            if (_consumableCount <= 0) _equippedConsumable = null;
            OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);
        }
    }
    private IEnumerator GradualHealRoutine(float totalHeal, float duration)
    {
        float healedAmount = 0;
        float healPerSecond = totalHeal / duration;

        while (healedAmount < totalHeal)
        {
            float step = healPerSecond * Time.deltaTime;
            _playerHealth.Heal(step);
            healedAmount += step;
            yield return null;
        }
    }
    private IEnumerator DamageBoostRoutine(float boostAmount, int durationInSeconds)
    {
        _playerStats.ApplyTemporaryDamageBoost(boostAmount);

        for (int i = 0; i < durationInSeconds; i++)
        {
            float Seconds = 1f;
            yield return new WaitForSeconds(Seconds);
            _playerHealth.TakeDirectDamage(2f);
        }

        _playerStats.ApplyTemporaryDamageBoost(0f);
    }
}
