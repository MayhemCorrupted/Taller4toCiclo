using UnityEngine;
using TMPro;

public class DescriptionUI : MonoBehaviour
{
    private SlotsUI _slotsUI;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemDescriptionText;
    [SerializeField] private TextMeshProUGUI _itemEffectText;
    private ItemData _currentItemInfo = null;
    private void Awake()
    {
        _slotsUI = FindAnyObjectByType<SlotsUI>();
        _slotsUI.OnItemSelected += ShowItemInfo;
    }
    private void OnDestroy()
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
            _currentItemInfo.ItemEffect.StringChanged -= UpdateItemEffect;
        }
        _currentItemInfo = item;

        if (_currentItemInfo != null)
        {
            _currentItemInfo.ItemName.StringChanged += UpdateItemName;
            _currentItemInfo.ItemDescription.StringChanged += UpdateItemDescription;
            _currentItemInfo.ItemEffect.StringChanged += UpdateItemEffect;
        }
        else
        {
            if (_itemNameText != null) _itemNameText.text = "";
            if (_itemDescriptionText != null) _itemDescriptionText.text = "";
            if (_itemEffectText != null) _itemEffectText.text = "";
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
    private void UpdateItemEffect(string text) 
    {
        if (_itemEffectText != null) _itemEffectText.text = text;
    }
}
