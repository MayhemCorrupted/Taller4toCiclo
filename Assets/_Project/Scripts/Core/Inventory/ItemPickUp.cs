using UnityEngine;
using UnityEngine.Localization;
public class ItemPickUp : MonoBehaviour, IInteractable
{
    [Header("Ítem")]
    [SerializeField] private ItemData _itemData;
    [SerializeField] private int _amount = 1;
    [Header("Prompt de Interacción")]
    [Tooltip("Texto localizado con '{key}' (Ej: '[{key}] Recoger').")]
    [SerializeField] private LocalizedString _interactionPrompt;
    public LocalizedString InteractionPrompt => _interactionPrompt;
    private InventoryManager _inventoryManager;
    private void Awake()
    {
        _inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
    public void Interact()
    {
        if (_itemData == null || _inventoryManager == null) return;
        _inventoryManager.AddItemToInventory(_itemData, _amount);
        gameObject.SetActive(false);
    }
}