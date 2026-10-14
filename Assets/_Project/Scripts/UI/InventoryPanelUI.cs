using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _firstSelectedSlot;

    [SerializeField] private TextMeshProUGUI _titleInventoryText;
    [SerializeField] private TextMeshProUGUI _titleStatsText;
    [SerializeField] private TextMeshProUGUI _titleObjectsText;
    [SerializeField] private TextMeshProUGUI _titleEffectText;

    [SerializeField] private LocalizedString _inventoryLocalized;
    [SerializeField] private LocalizedString _statsLocalized;
    [SerializeField] private LocalizedString _objectsLocalized;
    [SerializeField] private LocalizedString _effectLocalized;

    private PlayerInputs _playerInputs;
    private bool _isOpen = false;
    private void Awake()
    {
        _playerInputs = FindAnyObjectByType<PlayerInputs>();
        _inventoryPanel.SetActive(false);

        _inventoryLocalized.StringChanged += (text) => { if (_titleInventoryText != null) _titleInventoryText.text = text; };
        _statsLocalized.StringChanged += (text) => { if (_titleStatsText != null) _titleStatsText.text = text; };
        _objectsLocalized.StringChanged += (text) => { if (_titleObjectsText != null) _titleObjectsText.text = text; };
        _effectLocalized.StringChanged += (text) => { if (_titleEffectText != null) _titleEffectText.text = text; };
    }
    private void OnDestroy()
    {
        _inventoryLocalized.StringChanged -= (text) => { if (_titleInventoryText != null) _titleInventoryText.text = text; };
        _statsLocalized.StringChanged -= (text) => { if (_titleStatsText != null) _titleStatsText.text = text; };
        _objectsLocalized.StringChanged -= (text) => { if (_titleObjectsText != null) _titleObjectsText.text = text; };
        _effectLocalized.StringChanged -= (text) => { if (_titleEffectText != null) _titleEffectText.text = text; };
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnOpenInventory += ToggleInventory;
        }
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnOpenInventory -= ToggleInventory;
        }
    }
    private void ToggleInventory()
    {
        _isOpen = !_isOpen;

        _inventoryPanel.SetActive(_isOpen);

        if (_isOpen)
        {
            if (_firstSelectedSlot != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_firstSelectedSlot);
            }
        }
        if (_playerInputs.CurrentDeviceMode == "PC_KeyboardMouse")
        {
            Cursor.visible = _isOpen;
            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Confined;
        }
    }
}