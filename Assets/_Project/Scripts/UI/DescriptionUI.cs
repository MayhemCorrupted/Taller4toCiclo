using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class DescriptionUI : MonoBehaviour
{
    private SlotsUI _slotsUI;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemDescriptionText;
    private ItemData _currentItemInfo = null;
    private void Awake()
    {
        _slotsUI = FindAnyObjectByType<SlotsUI>();
    }
    private void OnEnable()
    {
        _slotsUI.OnItemSelected += ShowItemInfo;
    }
    private void OnDisable()
    {
        _slotsUI.OnItemSelected -= ShowItemInfo;
        ShowItemInfo(null);
    }
    private void ShowItemInfo(ItemData item)
    {
        if (_currentItemInfo != null)
        {
            _currentItemInfo.ItemName.StringChanged -= UpdateItemName;
            _currentItemInfo.ItemDescription.StringChanged -= UpdateItemDescription;
        }
        _currentItemInfo = item;

        if (_currentItemInfo != null)
        {
            _currentItemInfo.ItemName.StringChanged += UpdateItemName;
            _currentItemInfo.ItemDescription.StringChanged += UpdateItemDescription;
        }
        else
        {
            if (_itemNameText != null) _itemNameText.text = "";
            if (_itemDescriptionText != null) _itemDescriptionText.text = "";
        }
    }
    private void UpdateItemName(string localizedName)
    {
        if (_itemNameText != null) _itemNameText.text = localizedName;
    }
    private void UpdateItemDescription(string localizedDescription)
    {
        if (_itemDescriptionText != null) _itemDescriptionText.text = localizedDescription;
    }
}
