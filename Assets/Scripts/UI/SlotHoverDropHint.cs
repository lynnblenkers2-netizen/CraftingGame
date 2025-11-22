using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ItemSlotUI))]
public class SlotHoverDropHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image highlight;
    public Color canColor  = new Color(0f, 1f, 0f, 0.28f);
    public Color cantColor = new Color(1f, 0f, 0f, 0.28f);

    ItemSlotUI slot;

    void Awake()
    {
        slot = GetComponent<ItemSlotUI>();
        if (highlight) highlight.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!DragContext.Active || highlight == null) return;

        bool ok = WouldAccept(DragContext.Current.draggedItem, DragContext.Current.sourceSlot, slot);
        highlight.color = ok ? canColor : cantColor;
        highlight.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight) highlight.gameObject.SetActive(false);
    }

    bool WouldAccept(Item dragged, ItemSlotUI source, ItemSlotUI target)
    {
        if (dragged == null) return false;
        if (target == null) return false;

        if (source == target) return false;

        if (!target.AcceptsItem(dragged)) return false;

        var tStack = target.Stack;
        if (target.owner == ItemSlotUI.OwnerType.Inventory || target.owner == ItemSlotUI.OwnerType.Backpack)
            return true;

        if (target.owner == ItemSlotUI.OwnerType.Crafting)
            return tStack.IsEmpty || tStack.Item == dragged;

        return false;
    }

    public void HideNow()
    {
        if (highlight) highlight.gameObject.SetActive(false);
    }
}
