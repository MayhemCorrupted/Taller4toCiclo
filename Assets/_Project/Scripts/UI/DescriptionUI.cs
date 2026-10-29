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
        ShowItemInfo(null);
    }
    private void OnDestroy()
    {
        _slotsUI.OnItemSelected -= ShowItemInfo;
        ShowItemInfo(null);
    }
    private void ShowItemInfo(ItemData item)
    {
        LanguageManager.Instance.UnbindText(_itemNameText);
        LanguageManager.Instance.UnbindText(_itemDescriptionText);
        LanguageManager.Instance.UnbindText(_itemEffectText);

        _currentItemInfo = item;

        if (_currentItemInfo != null)
        {
            LanguageManager.Instance.BindText(_itemNameText, _currentItemInfo.ItemName);
            LanguageManager.Instance.BindText(_itemDescriptionText, _currentItemInfo.ItemDescription);
            LanguageManager.Instance.BindText(_itemEffectText, _currentItemInfo.ItemEffect);
        }
        else
        {
            if (_itemNameText != null) _itemNameText.text = "-";
            if (_itemDescriptionText != null) _itemDescriptionText.text = "-";
            if (_itemEffectText != null) _itemEffectText.text = "-";
        }
    }
}
