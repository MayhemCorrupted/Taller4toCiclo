using UnityEngine;
using UnityEngine.EventSystems;
public enum SelectedSlotType { None, General, Armor, Consumable, ArmorInventory }
public class SlotInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, 
IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private SlotsUI _uiManager;
    private SelectedSlotType _type;
    private int _index;
    public void Initialize(SlotsUI uiManager, SelectedSlotType type, int index = -1)
    {
        _uiManager = uiManager;
        _type = type;
        _index = index;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging) _uiManager.HandleSlotClick(_type, _index);
    }
    public void OnBeginDrag(PointerEventData eventData) => _uiManager.HandleBeginDrag(_index, _type, eventData);
    public void OnDrag(PointerEventData eventData) => _uiManager.HandleDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => _uiManager.HandleEndDrag();
    public void OnDrop(PointerEventData eventData) => _uiManager.HandleDrop(_type, _index);
    public void OnPointerEnter(PointerEventData eventData) => _uiManager.HandlePointerEnter(_type, _index);
    public void OnPointerExit(PointerEventData eventData) => _uiManager.HandlePointerExit();
    public void OnSelect(BaseEventData eventData) => _uiManager.HandlePointerEnter(_type, _index);
    public void OnDeselect(BaseEventData eventData) => _uiManager.HandlePointerExit();
}
