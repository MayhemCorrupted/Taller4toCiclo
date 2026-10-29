using UnityEngine;

public class ItemPickUp : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _itemData;
    [SerializeField] private int _amount = 1;

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
