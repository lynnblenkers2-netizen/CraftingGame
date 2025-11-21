using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ResourceEntryUI))]
public class ResourceHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    ResourceEntryUI entry;
    Coroutine showCoroutine;
    [SerializeField] float showDelay = 0.12f;

    void Awake()
    {
        entry = GetComponent<ResourceEntryUI>();
    }

    void OnDisable()
    {
        if (showCoroutine != null) { StopCoroutine(showCoroutine); showCoroutine = null; }
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (entry == null) return;
        if (ItemTooltipUI.Instance == null)
        {
            Debug.LogWarning("ItemTooltipUI.Instance not found in scene. Add ItemTooltipUI to a GameObject and assign tooltip prefab.");
            return;
        }

        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(DelayedShow(entry, eventData.position));
    }

    System.Collections.IEnumerator DelayedShow(ResourceEntryUI en, Vector2 pos)
    {
        float t = 0f;
        while (t < showDelay)
        {
            if (DragContext.Active) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        ItemTooltipUI.Instance.Show(en, pos);
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
}
