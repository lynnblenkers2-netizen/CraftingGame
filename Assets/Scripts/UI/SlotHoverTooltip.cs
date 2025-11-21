using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemSlotUI))]
public class SlotHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IBeginDragHandler
{
    ItemSlotUI slot;
    Coroutine showCoroutine;
    [SerializeField] float showDelay = 0.12f;

    void Awake()
    {
        slot = GetComponent<ItemSlotUI>();
    }

    void OnDisable()
    {
        if (showCoroutine != null) { StopCoroutine(showCoroutine); showCoroutine = null; }
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slot == null) return;
        var st = slot.Stack;
        if (st == null || st.IsEmpty) return;

        if (ItemTooltipUI.Instance == null)
        {
            Debug.LogWarning("ItemTooltipUI.Instance not found in scene. Add ItemTooltipUI to a GameObject and assign tooltip prefab.");
            return;
        }

        // start delayed show
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(DelayedShow(st, eventData.position));
    }

    System.Collections.IEnumerator DelayedShow(ItemStack st, Vector2 screenPos)
    {
        float t = 0f;
        while (t < showDelay)
        {
            if (DragContext.Active) yield break; // cancel if a drag started
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        ItemTooltipUI.Instance.Show(st, screenPos);
        showCoroutine = null;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.UpdatePosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (showCoroutine != null) { StopCoroutine(showCoroutine); showCoroutine = null; }
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // hide immediately when dragging starts
        if (showCoroutine != null) { StopCoroutine(showCoroutine); showCoroutine = null; }
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.Hide();
    }
}
