using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]  public class InventorySlot
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
    public event Action<ItemData, int> OnItemAdded;

    [Header("Catálogo de Ítems (Reconstrucción al Cargar)")]
    [Tooltip("Arrastra aquí todos los ScriptableObjects de ítems (Armaduras y Consumibles) para restaurarlos por ItemID.")]
    [SerializeField] private ItemData[] _itemCatalog;

    [Header("Slots de Inventario")]
    [SerializeField] private InventorySlot[] _generalSlots = new InventorySlot[6];
    [SerializeField] private InventorySlot[] _armorInventorySlots = new InventorySlot[3];

    public InventorySlot[] GeneralSlots => _generalSlots;
    public InventorySlot[] ArmorInventorySlots => _armorInventorySlots;
    public ArmorItem EquippedArmor => _equippedArmor;
    public ConsumableItem EquippedConsumable => _equippedConsumable;
    public int ConsumableCount => _consumableCount;

    private ArmorItem _equippedArmor;
    private ConsumableItem _equippedConsumable;
    private int _consumableCount = 0;

    private readonly Dictionary<string, ItemData> _itemLookup = new(StringComparer.OrdinalIgnoreCase);
    private PlayerInputs _playerInputs;
    private PlayerStats _playerStats;
    private PlayerHealth _playerHealth;
    private PlayerFSM _playerFSM;
    private Coroutine _activeBuffCoroutine;
    private Coroutine _activeHealCoroutine;
    private void Awake()
    {
        _playerInputs = FindAnyObjectByType<PlayerInputs>(FindObjectsInactive.Include);
        _playerStats = FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include);
        _playerHealth = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        _playerFSM = FindAnyObjectByType<PlayerFSM>(FindObjectsInactive.Include);

        BuildItemLookup();

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            _generalSlots[i] = new InventorySlot();
        }
        for (int i = 0; i < _armorInventorySlots.Length; i++)
        {
            _armorInventorySlots[i] = new InventorySlot();
        }

        SyncFromSaveManager();
    }
    private void OnEnable()
    {
        if (_playerInputs != null) _playerInputs.OnUseItem += UseEquippedConsumable;
    }
    private void OnDisable()
    {
        if (_playerInputs != null) _playerInputs.OnUseItem -= UseEquippedConsumable;
    }
    private void Start()
    {
        if (_playerStats == null)
            _playerStats = FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include);

        if (_playerStats != null)
        {
            _playerStats._armorDefense = _equippedArmor != null ? _equippedArmor.DefenseBonus : 0f;
            _playerStats.RecalculateStats();
        }
    }
    private static string GetEffectiveItemID(ItemData item)
    {
        if (item == null) return string.Empty;
        return !string.IsNullOrWhiteSpace(item.ItemID) ? item.ItemID.Trim() : item.name.Trim();
    }
    private void RegisterItemReference(ItemData item)
    {
        if (item == null) return;

        if (!string.IsNullOrWhiteSpace(item.ItemID))
            _itemLookup[item.ItemID.Trim()] = item;

        if (!string.IsNullOrWhiteSpace(item.name))
            _itemLookup[item.name.Trim()] = item;

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterKnownItem(item);
    }

    private void BuildItemLookup()
    {
        _itemLookup.Clear();

        if (_itemCatalog != null)
        {
            for (int i = 0; i < _itemCatalog.Length; i++)
            {
                RegisterItemReference(_itemCatalog[i]);
            }
        }

        ItemData[] resourceItems = Resources.LoadAll<ItemData>(string.Empty);
        for (int i = 0; i < resourceItems.Length; i++)
        {
            RegisterItemReference(resourceItems[i]);
        }
    }
    private ItemData ResolveItemByID(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return null;

        string key = itemID.Trim();
        if (_itemLookup.TryGetValue(key, out ItemData found))
            return found;

        if (SaveManager.Instance != null)
        {
            ItemData cached = SaveManager.Instance.ResolveCachedItem(key);
            if (cached != null)
            {
                _itemLookup[key] = cached;
                return cached;
            }
        }

        GameLogger.LogWarning($"[InventoryManager] No se encontró ItemData con ID '{key}'. Asegúrate de asignarlo en _itemCatalog o Resources.");
        return null;
    }

    private void SyncFromSaveManager()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasActiveSession || SaveManager.Instance.CurrentRun == null)
            return;

        ImportInventoryData(SaveManager.Instance.CurrentRun.inventory);
    }
    private void NotifyInventoryStateChanged()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SyncInventoryFromManager(this);
        }
    }
    public void ExportInventoryData(InventorySaveData targetData)
    {
        if (targetData == null) return;

        if (_equippedArmor != null) RegisterItemReference(_equippedArmor);
        if (_equippedConsumable != null) RegisterItemReference(_equippedConsumable);

        targetData.equippedArmorID = _equippedArmor != null ? GetEffectiveItemID(_equippedArmor) : string.Empty;
        targetData.equippedConsumableID = _equippedConsumable != null ? GetEffectiveItemID(_equippedConsumable) : string.Empty;
        targetData.equippedConsumableCount = _equippedConsumable != null ? _consumableCount : 0;

        targetData.generalSlots.Clear();
        for (int i = 0; i < _generalSlots.Length; i++)
        {
            if (!_generalSlots[i].IsEmpty)
            {
                RegisterItemReference(_generalSlots[i].Item);
                string resolvedID = GetEffectiveItemID(_generalSlots[i].Item);
                if (!string.IsNullOrWhiteSpace(resolvedID))
                {
                    targetData.generalSlots.Add(new SerializedInventorySlot
                    {
                        slotIndex = i,
                        itemID = resolvedID,
                        quantity = _generalSlots[i].Quantity
                    });
                }
            }
        }

        targetData.armorSlots.Clear();
        for (int i = 0; i < _armorInventorySlots.Length; i++)
        {
            if (!_armorInventorySlots[i].IsEmpty)
            {
                RegisterItemReference(_armorInventorySlots[i].Item);
                string resolvedID = GetEffectiveItemID(_armorInventorySlots[i].Item);
                if (!string.IsNullOrWhiteSpace(resolvedID))
                {
                    targetData.armorSlots.Add(new SerializedInventorySlot
                    {
                        slotIndex = i,
                        itemID = resolvedID,
                        quantity = _armorInventorySlots[i].Quantity
                    });
                }
            }
        }
    }
    public void ImportInventoryData(InventorySaveData sourceData)
    {
        if (sourceData == null) return;

        for (int i = 0; i < _generalSlots.Length; i++) _generalSlots[i].Clear();
        for (int i = 0; i < _armorInventorySlots.Length; i++) _armorInventorySlots[i].Clear();

        _equippedArmor = ResolveItemByID(sourceData.equippedArmorID) as ArmorItem;
        if (_playerStats != null)
        {
            _playerStats._armorDefense = _equippedArmor != null ? _equippedArmor.DefenseBonus : 0f;
            _playerStats.RecalculateStats();
        }
        OnArmorEquipped?.Invoke(_equippedArmor);

        ConsumableItem loadedConsumable = ResolveItemByID(sourceData.equippedConsumableID) as ConsumableItem;
        if (loadedConsumable != null && sourceData.equippedConsumableCount > 0)
        {
            _equippedConsumable = loadedConsumable;
            _consumableCount = sourceData.equippedConsumableCount;
        }
        else
        {
            _equippedConsumable = null;
            _consumableCount = 0;
        }
        OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);

        if (sourceData.generalSlots != null)
        {
            for (int i = 0; i < sourceData.generalSlots.Count; i++)
            {
                SerializedInventorySlot savedSlot = sourceData.generalSlots[i];
                if (savedSlot.slotIndex < 0 || savedSlot.slotIndex >= _generalSlots.Length) continue;

                ItemData item = ResolveItemByID(savedSlot.itemID);
                if (item != null && savedSlot.quantity > 0)
                {
                    _generalSlots[savedSlot.slotIndex].Item = item;
                    _generalSlots[savedSlot.slotIndex].Quantity = savedSlot.quantity;
                }
            }
        }
        OnInventoryUpdated?.Invoke(_generalSlots);

        if (sourceData.armorSlots != null)
        {
            for (int i = 0; i < sourceData.armorSlots.Count; i++)
            {
                SerializedInventorySlot savedSlot = sourceData.armorSlots[i];
                if (savedSlot.slotIndex < 0 || savedSlot.slotIndex >= _armorInventorySlots.Length) continue;

                ItemData item = ResolveItemByID(savedSlot.itemID);
                if (item != null && savedSlot.quantity > 0)
                {
                    _armorInventorySlots[savedSlot.slotIndex].Item = item;
                    _armorInventorySlots[savedSlot.slotIndex].Quantity = savedSlot.quantity;
                }
            }
        }
        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
    }
    public void AddItem(ItemData item)
    {
        TryAddItem(item, 1);
    }
    public void AddItemToInventory(ItemData item, int amount = 1)
    {
        TryAddItem(item, amount);
    }
    public bool TryAddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        RegisterItemReference(item);
        bool added;

        if (item is ArmorItem)
        {
            added = AddToSpecificArray(_armorInventorySlots, item, amount, OnArmorInventoryUpdated, allowStacking: false);
        }
        else if (item is ConsumableItem consumable)
        {
            if (_equippedConsumable != null && GetEffectiveItemID(_equippedConsumable) == GetEffectiveItemID(consumable))
            {
                _consumableCount += amount;
                OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);
                added = true;
            }
            else
            {
                added = AddToSpecificArray(_generalSlots, item, amount, OnInventoryUpdated, allowStacking: true);
            }
        }
        else
        {
            added = AddToSpecificArray(_generalSlots, item, amount, OnInventoryUpdated, allowStacking: true);
        }

        if (added)
        {
            OnItemAdded?.Invoke(item, amount);
            NotifyInventoryStateChanged();
        }
        else
        {
            GameLogger.LogWarning($"[InventoryManager] Inventario lleno. No se pudo añadir '{item.name}' (x{amount}).");
        }

        return added;
    }
    private bool AddToSpecificArray(InventorySlot[] slotArray, ItemData item, int amount, Action<InventorySlot[]> updateEvent, bool allowStacking)
    {
        if (allowStacking)
        {
            string targetID = GetEffectiveItemID(item);
            InventorySlot existingSlot = slotArray.FirstOrDefault(s => !s.IsEmpty && GetEffectiveItemID(s.Item) == targetID);
            if (existingSlot != null)
            {
                existingSlot.Quantity += amount;
                updateEvent?.Invoke(slotArray);
                return true;
            }

            InventorySlot emptySlot = slotArray.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot != null)
            {
                emptySlot.Item = item;
                emptySlot.Quantity = amount;
                updateEvent?.Invoke(slotArray);
                return true;
            }

            return false;
        }
        else
        {
            int freeSlots = slotArray.Count(s => s.IsEmpty);
            if (freeSlots < amount) return false;

            int remaining = amount;
            for (int i = 0; i < slotArray.Length && remaining > 0; i++)
            {
                if (slotArray[i].IsEmpty)
                {
                    slotArray[i].Item = item;
                    slotArray[i].Quantity = 1;
                    remaining--;
                }
            }

            updateEvent?.Invoke(slotArray);
            return true;
        }
    }
    public void EquipArmorFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _armorInventorySlots.Length) return;

        InventorySlot slot = _armorInventorySlots[slotIndex];
        if (slot.IsEmpty || slot.Item is not ArmorItem armor) return;

        ArmorItem previousArmor = _equippedArmor;

        _equippedArmor = armor;
        if (_playerStats != null)
        {
            _playerStats._armorDefense = armor.DefenseBonus;
            _playerStats.RecalculateStats();
        }

        OnArmorEquipped?.Invoke(_equippedArmor);

        if (previousArmor != null)
        {
            slot.Item = previousArmor;
            slot.Quantity = 1;
        }
        else slot.Clear();

        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
        NotifyInventoryStateChanged();
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
        NotifyInventoryStateChanged();
    }
    public void SwapGeneralSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _generalSlots.Length || indexB < 0 || indexB >= _generalSlots.Length) return;

        (_generalSlots[indexB], _generalSlots[indexA]) = (_generalSlots[indexA], _generalSlots[indexB]);
        OnInventoryUpdated?.Invoke(_generalSlots);
        NotifyInventoryStateChanged();
    }
    public void SwapArmorInventorySlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _armorInventorySlots.Length || indexB < 0 || indexB >= _armorInventorySlots.Length) return;

        (_armorInventorySlots[indexB], _armorInventorySlots[indexA]) = (_armorInventorySlots[indexA], _armorInventorySlots[indexB]);
        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
        NotifyInventoryStateChanged();
    }
    public void UnequipArmor(int targetSlotIndex)
    {
        if (_equippedArmor == null || targetSlotIndex < 0 || targetSlotIndex >= _armorInventorySlots.Length) return;
        if (!_armorInventorySlots[targetSlotIndex].IsEmpty) return;

        _armorInventorySlots[targetSlotIndex].Item = _equippedArmor;
        _armorInventorySlots[targetSlotIndex].Quantity = 1;

        _equippedArmor = null;
        if (_playerStats != null)
        {
            _playerStats._armorDefense = 0;
            _playerStats.RecalculateStats();
        }

        OnArmorEquipped?.Invoke(null);
        OnArmorInventoryUpdated?.Invoke(_armorInventorySlots);
        NotifyInventoryStateChanged();
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
        NotifyInventoryStateChanged();
    }
    private void UseEquippedConsumable()
    {
        if (_equippedConsumable == null || _consumableCount <= 0) return;
        if (_playerFSM == null) return;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running)
        {
            return;
        }

        if (_equippedConsumable.EffectType == ConsumableEffectType.Heal &&
            _playerHealth != null && _playerHealth.CurrentHealth >= _playerHealth.MaxHealth)
        {
            GameLogger.LogInfo("[Inventory] La salud ya está al máximo.");
            return;
        }

        _playerFSM.ChangeState(PlayerState.UsingItem);
    }
    public void ApplyConsumableEffect()
    {
        if (_equippedConsumable == null || _consumableCount <= 0) return;

        bool wasUsed = false;

        switch (_equippedConsumable.EffectType)
        {
            case ConsumableEffectType.Heal:
                if (_playerHealth != null && _playerHealth.CurrentHealth < _playerHealth.MaxHealth)
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
                StatsUI statsUI = FindAnyObjectByType<StatsUI>(FindObjectsInactive.Include);
                if (statsUI != null) statsUI.GrantStatPoints(_consumableCount);
                wasUsed = true;
                break;
        }
        if (wasUsed)
        {
            _consumableCount--;
            if (_consumableCount <= 0) _equippedConsumable = null;

            OnConsumableEquipped?.Invoke(_equippedConsumable, _consumableCount);
            NotifyInventoryStateChanged();
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
        float second = 1;

        for (int i = 0; i < durationInSeconds; i++)
        {
            yield return new WaitForSeconds(second);
            _playerHealth.TakeDirectDamage(2f);
        }

        _playerStats.ApplyTemporaryDamageBoost(0f);
    }
}