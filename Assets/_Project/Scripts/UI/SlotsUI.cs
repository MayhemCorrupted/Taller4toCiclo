using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class SlotsUI : MonoBehaviour
{
    public event Action<ItemData> OnItemSelected;
    private InventoryManager _inventoryManager;

    [SerializeField] private Button[] _generalSlotButtons = new Button[6];
    [SerializeField] private Image[] _generalSlotIcons = new Image[6];
    [SerializeField] private TextMeshProUGUI[] _generalSlotCounts = new TextMeshProUGUI[6];

    [SerializeField] private Button[] _armorInvSlotButtons = new Button[3];
    [SerializeField] private Image[] _armorInvSlotIcons = new Image[3];

    [SerializeField] private Button _armorButton;
    [SerializeField] private Image _armorIcon;

    [SerializeField] private Button _consumableButton;
    [SerializeField] private Image _consumableIcon;
    [SerializeField] private TextMeshProUGUI _consumableCountText;

    [SerializeField] private Image _dragIcon;
    [SerializeField] private Color _selectedColor = Color.green;
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _emptyColor = new(1, 1, 1, 0);

    private SelectedSlotType _currentSelectionType = SelectedSlotType.None;
    private int _selectedGeneralIndex = -1;
    private bool _isDragging = false;
    private SelectedSlotType _dragSourceType;
    private int _dragSourceIndex;
    private void Awake()
    {
        _inventoryManager = FindAnyObjectByType<InventoryManager>();

        for (int i = 0; i < _generalSlotButtons.Length; i++)
            _generalSlotButtons[i].gameObject.AddComponent<SlotInteraction>().Initialize(this, SelectedSlotType.General, i);

        for (int i = 0; i < _armorInvSlotButtons.Length; i++)
            _armorInvSlotButtons[i].gameObject.AddComponent<SlotInteraction>().Initialize(this, SelectedSlotType.ArmorInventory, i);

        _armorButton.gameObject.AddComponent<SlotInteraction>().Initialize(this, SelectedSlotType.Armor);
        _consumableButton.gameObject.AddComponent<SlotInteraction>().Initialize(this, SelectedSlotType.Consumable);

        if (_dragIcon != null)
        {
            _dragIcon.gameObject.SetActive(false);
            _dragIcon.raycastTarget = false;
        }
        ClearSelection();
    }
    private void OnEnable()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.OnInventoryUpdated += UpdateGeneralSlots;
            _inventoryManager.OnArmorEquipped += UpdateArmorSlot;
            _inventoryManager.OnConsumableEquipped += UpdateConsumableSlot;
            _inventoryManager.OnArmorInventoryUpdated += UpdateArmorInventorySlots;

            UpdateGeneralSlots(_inventoryManager.GeneralSlots);
            UpdateArmorInventorySlots(_inventoryManager.ArmorInventorySlots);
            UpdateArmorSlot(_inventoryManager.EquippedArmor);
            UpdateConsumableSlot(_inventoryManager.EquippedConsumable, _inventoryManager.ConsumableCount);
        }
    }
    private void OnDisable()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.OnInventoryUpdated -= UpdateGeneralSlots;
            _inventoryManager.OnArmorEquipped -= UpdateArmorSlot;
            _inventoryManager.OnConsumableEquipped -= UpdateConsumableSlot;
            _inventoryManager.OnArmorInventoryUpdated -= UpdateArmorInventorySlots;
        }
    }
    public void HandlePointerEnter(SelectedSlotType type, int index)
    {
        if (_isDragging) return;
        ItemData item = GetItemData(type, index);
        if (item != null)
        {
            OnItemSelected?.Invoke(item);
        }
    }
    public void HandlePointerExit()
    {
        if (_isDragging) return;
        if (_currentSelectionType != SelectedSlotType.None)
            OnItemSelected?.Invoke(GetItemData(_currentSelectionType, _selectedGeneralIndex));
        else
            OnItemSelected?.Invoke(null);

    }
    public void HandleBeginDrag(int index, SelectedSlotType type, PointerEventData eventData)
    {
        ItemData item = GetItemData(type, index);
        if (item == null) return;
        _isDragging = true;
        _dragSourceType = type;
        _dragSourceIndex = index;

        if (_dragIcon != null)
        {
            _dragIcon.sprite = item.Icon;
            _dragIcon.gameObject.SetActive(true);
            _dragIcon.transform.position = eventData.position;
        }

        SetIconVisibility(type, index, false);
        ClearSelection();
        OnItemSelected?.Invoke(null);
    }
    public void HandleDrag(PointerEventData eventData)
    {
        if (_isDragging && _dragIcon != null)
            _dragIcon.transform.position = eventData.position;
        SetIconVisibility(_dragSourceType, _dragSourceIndex, true);
    }
    public void HandleEndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (_dragIcon != null) _dragIcon.gameObject.SetActive(false);
        SetIconVisibility(_dragSourceType, _dragSourceIndex, true);
    }
    public void HandleDrop(SelectedSlotType targetType, int targetIndex)
    {
        if (!_isDragging) return;
        ProcessAction(_dragSourceType, _dragSourceIndex, targetType, targetIndex);
    }
    public void HandleSlotClick(SelectedSlotType type, int index)
    {
        if (_isDragging) return;

        if (_currentSelectionType == SelectedSlotType.None)
        {
            if (GetItemData(type, index) != null)
                SetSelection(type, index);
        }
        else
        {
            ProcessAction(_currentSelectionType, _selectedGeneralIndex, type, index);
            ClearSelection();
        }
    }
    private void ProcessAction(SelectedSlotType source, int sourceIdx, SelectedSlotType target, int targetIdx)
    {
        if (source == target && sourceIdx == targetIdx) return;
        ItemData sourceItem = GetItemData(source, sourceIdx);
        ItemData targetItem = GetItemData(target, targetIdx);

        if (source == SelectedSlotType.General && target == SelectedSlotType.General)
        {
            _inventoryManager.SwapGeneralSlots(sourceIdx, targetIdx);
        }
        else if (source == SelectedSlotType.General && target == SelectedSlotType.Consumable)
        {
            if (sourceItem is ConsumableItem) _inventoryManager.EquipConsumableFromSlot(sourceIdx);
        }
        else if (source == SelectedSlotType.Consumable && target == SelectedSlotType.General)
        {
            if (targetItem == null) _inventoryManager.UnequipConsumable(targetIdx);
            else if (targetItem is ConsumableItem) _inventoryManager.EquipConsumableFromSlot(targetIdx);
        }
        else if (source == SelectedSlotType.ArmorInventory && target == SelectedSlotType.ArmorInventory)
        {
            _inventoryManager.SwapArmorInventorySlots(sourceIdx, targetIdx);
        }
        else if (source == SelectedSlotType.ArmorInventory && target == SelectedSlotType.Armor)
        {
            if (sourceItem is ArmorItem) _inventoryManager.EquipArmorFromSlot(sourceIdx);
        }
        else if (source == SelectedSlotType.Armor && target == SelectedSlotType.ArmorInventory)
        {
            if (targetItem == null) _inventoryManager.UnequipArmor(targetIdx);
            else if (targetItem is ArmorItem) _inventoryManager.EquipArmorFromSlot(targetIdx);
        }
    }
    private ItemData GetItemData(SelectedSlotType type, int index)
    {
        if (type == SelectedSlotType.General) return _inventoryManager.GeneralSlots[index].Item;
        if (type == SelectedSlotType.ArmorInventory) return _inventoryManager.ArmorInventorySlots[index].Item;
        if (type == SelectedSlotType.Armor) return _inventoryManager.EquippedArmor;
        if (type == SelectedSlotType.Consumable) return _inventoryManager.EquippedConsumable;
        return null;
    }
    private void SetIconVisibility(SelectedSlotType type, int index, bool isVisible)
    {
        Color color = isVisible ? _defaultColor : _emptyColor;

        if (type == SelectedSlotType.General) _generalSlotIcons[index].color = color;
        else if (type == SelectedSlotType.ArmorInventory) _armorInvSlotIcons[index].color = color;
        else if (type == SelectedSlotType.Armor) _armorIcon.color = color;
        else if (type == SelectedSlotType.Consumable) _consumableIcon.color = color;
    }
    private void ClearSelection()
    {
        if (_currentSelectionType == SelectedSlotType.General && _selectedGeneralIndex >= 0)
        {
            bool isEmpty = _inventoryManager.GeneralSlots[_selectedGeneralIndex].IsEmpty;
            _generalSlotButtons[_selectedGeneralIndex].image.color = isEmpty ? _emptyColor : _defaultColor;
        }
        else if (_currentSelectionType == SelectedSlotType.ArmorInventory && _selectedGeneralIndex >= 0)
        {
            bool isEmpty = _inventoryManager.ArmorInventorySlots[_selectedGeneralIndex].IsEmpty;
            _armorInvSlotButtons[_selectedGeneralIndex].image.color = isEmpty ? _emptyColor : _defaultColor;
        }
        else if (_currentSelectionType == SelectedSlotType.Armor)
        {
            bool isEmpty = _inventoryManager.EquippedArmor == null;
            _armorButton.image.color = isEmpty ? _emptyColor : _defaultColor;
        }
        else if (_currentSelectionType == SelectedSlotType.Consumable)
        {
            bool isEmpty = _inventoryManager.EquippedConsumable == null;
            _consumableButton.image.color = isEmpty ? _emptyColor : _defaultColor;
        }

        _currentSelectionType = SelectedSlotType.None;
        _selectedGeneralIndex = -1;
        OnItemSelected?.Invoke(null);
    }
    private void SetSelection(SelectedSlotType type, int index)
    {
        ClearSelection();
        _currentSelectionType = type;
        _selectedGeneralIndex = index;

        if (type == SelectedSlotType.General) _generalSlotButtons[index].image.color = _selectedColor;
        else if (type == SelectedSlotType.ArmorInventory) _armorInvSlotButtons[index].image.color = _selectedColor;
        else if (type == SelectedSlotType.Armor) _armorButton.image.color = _selectedColor;
        else if (type == SelectedSlotType.Consumable) _consumableButton.image.color = _selectedColor;

        OnItemSelected?.Invoke(GetItemData(type, index));
    }
    private void UpdateGeneralSlots(InventorySlot[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                _generalSlotIcons[i].sprite = null;
                _generalSlotIcons[i].color = _emptyColor;
                _generalSlotCounts[i].text = "";
            }
            else
            {
                _generalSlotIcons[i].sprite = slots[i].Item.Icon;
                _generalSlotIcons[i].color = _defaultColor;
                _generalSlotCounts[i].text = slots[i].Quantity > 1 ? slots[i].Quantity.ToString() : "";
            }
        }
        ClearSelection();
    }
    private void UpdateArmorInventorySlots(InventorySlot[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                _armorInvSlotIcons[i].sprite = null;
                _armorInvSlotIcons[i].color = _emptyColor;
            }
            else
            {
                _armorInvSlotIcons[i].sprite = slots[i].Item.Icon;
                _armorInvSlotIcons[i].color = _defaultColor;
            }
        }
        ClearSelection();
    }
    private void UpdateArmorSlot(ArmorItem armor)
    {
        if (armor != null)
        {
            _armorIcon.sprite = armor.Icon;
            _armorIcon.color = _defaultColor;
        }
        else
        {
            _armorIcon.sprite = null;
            _armorIcon.color = _emptyColor;
        }
        ClearSelection();
    }
    private void UpdateConsumableSlot(ConsumableItem consumable, int count)
    {
        if (consumable != null && count > 0)
        {
            _consumableIcon.sprite = consumable.Icon;
            _consumableIcon.color = _defaultColor;
            _consumableCountText.text = count.ToString();
        }
        else
        {
            _consumableIcon.sprite = null;
            _consumableIcon.color = _emptyColor;
            _consumableCountText.text = "";
        }
        ClearSelection();
    }
}
