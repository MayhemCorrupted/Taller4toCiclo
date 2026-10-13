using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum SelectedSlotType { None, General, Armor, Consumable }
public class SlotsUI : MonoBehaviour
{
    public event Action<ItemData> OnItemSelected;
    private InventoryManager _inventoryManager;
    [SerializeField] private Button[] _generalSlotButtons = new Button[6];
    [SerializeField] private Image[] _generalSlotIcons = new Image[6];
    [SerializeField] private TextMeshProUGUI[] _generalSlotCounts = new TextMeshProUGUI[6];

    [SerializeField] private Button _armorButton;
    [SerializeField] private Image _armorIcon;

    [SerializeField] private Button _consumableButton;
    [SerializeField] private Image _consumableIcon;
    [SerializeField] private TextMeshProUGUI _consumableCountText;

    [SerializeField] private Color _selectedColor = Color.green;
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _emptyColor = new(1, 1, 1, 0);

    private SelectedSlotType _currentSelectionType = SelectedSlotType.None;
    private int _selectedGeneralIndex = -1;
    private void Awake()
    {
        _inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
    private void OnEnable()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.OnInventoryUpdated += UpdateGeneralSlots;
            _inventoryManager.OnArmorEquipped += UpdateArmorSlot;
            _inventoryManager.OnConsumableEquipped += UpdateConsumableSlot;
        }
        for (int i = 0; i < _generalSlotButtons.Length; i++)
        {
            int index = i;
            _generalSlotButtons[i].onClick.AddListener(() => HandleGeneralSlotClick(index));
        }
        _armorButton.onClick.AddListener(HandleArmorSlotClick);
        _consumableButton.onClick.AddListener(HandleConsumableSlotClick);
    }
    private void OnDisable()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.OnInventoryUpdated -= UpdateGeneralSlots;
            _inventoryManager.OnArmorEquipped -= UpdateArmorSlot;
            _inventoryManager.OnConsumableEquipped -= UpdateConsumableSlot;
        }

        foreach (var btn in _generalSlotButtons) btn.onClick.RemoveAllListeners();
        _armorButton.onClick.RemoveAllListeners();
        _consumableButton.onClick.RemoveAllListeners();
    }
    private void HandleGeneralSlotClick(int index)
    {
        if (_currentSelectionType == SelectedSlotType.None)
        {
            if (!_inventoryManager.GeneralSlots[index].IsEmpty)
            {
                SetSelection(SelectedSlotType.General, index);
            }
        }
        else if (_currentSelectionType == SelectedSlotType.General)
        {
            _inventoryManager.SwapGeneralSlots(_selectedGeneralIndex, index);
            ClearSelection();
        }
        else if (_currentSelectionType == SelectedSlotType.Armor)
        {
            _inventoryManager.UnequipArmor(index);
            ClearSelection();
        }
        else if (_currentSelectionType == SelectedSlotType.Consumable)
        {
            _inventoryManager.UnequipConsumable(index);
            ClearSelection();
        }
    }
    private void HandleArmorSlotClick()
    {
        if (_currentSelectionType == SelectedSlotType.None)
        {
            if (_inventoryManager.EquippedArmor != null)
            {
                SetSelection(SelectedSlotType.Armor, -1);
            }
        }
        else if (_currentSelectionType == SelectedSlotType.General)
        {
            _inventoryManager.EquipFromSlot(_selectedGeneralIndex);
            ClearSelection();
        }
        else
        {
            ClearSelection();
        }
    }
    private void HandleConsumableSlotClick()
    {
        if (_currentSelectionType == SelectedSlotType.None)
        {
            if (_inventoryManager.EquippedConsumable != null)
            {
                SetSelection(SelectedSlotType.Consumable, -1);
            }
        }
        else if (_currentSelectionType == SelectedSlotType.General)
        {
            _inventoryManager.EquipFromSlot(_selectedGeneralIndex);
            ClearSelection();
        }
        else
        {
            ClearSelection();
        }
    }
    private void SetSelection(SelectedSlotType type, int index)
    {
        ClearSelection();
        _currentSelectionType = type;
        _selectedGeneralIndex = index;

        ItemData selectedItem = null;

        if (type == SelectedSlotType.General)
        {
            _generalSlotButtons[index].image.color = _selectedColor;
            selectedItem = _inventoryManager.GeneralSlots[index].Item;
        }
        else if (type == SelectedSlotType.Armor)
        {
            _armorButton.image.color = _selectedColor;
            selectedItem = _inventoryManager.EquippedArmor;
        }
        else if (type == SelectedSlotType.Consumable)
        {
            _consumableButton.image.color = _selectedColor;
            selectedItem = _inventoryManager.EquippedConsumable;
        }
        OnItemSelected?.Invoke(selectedItem);
    }
    private void ClearSelection()
    {
        if (_currentSelectionType == SelectedSlotType.General && _selectedGeneralIndex >= 0)
        {
            bool isEmpty = _inventoryManager.GeneralSlots[_selectedGeneralIndex].IsEmpty;
            _generalSlotButtons[_selectedGeneralIndex].image.color = isEmpty ? _emptyColor : _defaultColor;
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
