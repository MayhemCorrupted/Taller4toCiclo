using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryPanelUI : MonoBehaviour, IPanel
{
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _firstSelectedSlot;
    [SerializeField] private GameObject _renderPreviewCamera;

    [SerializeField] private LocalizedTMP[] _localizedElements;
    private void Awake()
    {
        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(false);
    }
    private void Start()
    {
        LanguageManager.Instance.RegisterElements(_localizedElements);
    }
    private void OnDestroy()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.UnregisterElements(_localizedElements);
        }
    }
    public void OpenPanel()
    {
        if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(true);

        if (_firstSelectedSlot != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_firstSelectedSlot);
        }
    }
    public void ClosePanel()
    {
        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
        if (_renderPreviewCamera != null) _renderPreviewCamera.SetActive(false);
    }
}