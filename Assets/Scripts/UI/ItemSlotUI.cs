using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public enum OwnerType { Inventory, Crafting, Backpack }

    [Header("Slot-Zuordnung")]
    public OwnerType owner;
    public int index;
    [System.NonSerialized]
    public Inventory inventory;
    public CraftingGrid craftingGrid;

    [Header("UI")]
    public Image iconImage;
    public Text countText;
    public TextMeshProUGUI countTMP;

    Canvas rootCanvas;
    CanvasGroup canvasGroup;
    RectTransform dragIcon;
    Image dragIconImage;
    Text dragCountTextLegacy;
    TextMeshProUGUI dragCountTMP;

    bool isInitialized;
    bool isLocked;

    static bool IsInventoryOwner(OwnerType type) => type == OwnerType.Inventory || type == OwnerType.Backpack;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public ItemStack Stack
    {
        get
        {
            if (!isInitialized) return new ItemStack();
            if (IsInventoryOwner(owner))
                return inventory != null ? inventory.GetSlot(index) : new ItemStack();
            return craftingGrid != null ? craftingGrid.GetCell(index) : new ItemStack();
        }
    }

    public void Init(Inventory inv, int idx, OwnerType type)
    {
        inventory = inv;
        owner = type;
        index = idx;
        craftingGrid = null;
        InitializeVisuals();
    }

    public void Init(Inventory inv, OwnerType type, int idx) => Init(inv, idx, type);

    public void Init(CraftingGrid gridRef, int idx)
    {
        craftingGrid = gridRef;
        owner = OwnerType.Crafting;
        index = idx;
        inventory = null;
        InitializeVisuals();
    }

    void InitializeVisuals()
    {
        if (!iconImage) iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (!countTMP) countTMP = transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
        if (!countTMP) countTMP = GetComponentInChildren<TextMeshProUGUI>(true);

        if (!countTMP)
        {
            if (!countText) countText = transform.Find("Count")?.GetComponent<Text>();
            if (!countText) countText = GetComponentInChildren<Text>(true);
        }
        else
        {
            var legacy = GetComponentInChildren<Text>(true);
            if (legacy) legacy.enabled = false;
        }

        isInitialized = (IsInventoryOwner(owner) && inventory != null)
                     || (owner == OwnerType.Crafting && craftingGrid != null);

        if (countTMP) countTMP.text = "";
        if (countText) countText.text = "";

        Refresh();
    }

    public void Refresh()
    {
        if (!isInitialized || iconImage == null) return;

        var st = Stack;
        bool has = !st.IsEmpty;

        iconImage.sprite = has ? st.Item.Icon : null;
        iconImage.enabled = has && iconImage.sprite != null;

        string countStr = (has && st.Amount > 1) ? st.Amount.ToString() : "";
        if (countTMP) countTMP.text = countStr;
        if (countText) countText.text = countStr;

        var pop = transform.Find("Count")?.GetComponent<CountPopAnimator>();
        if (pop)
        {
            int amt = st.IsEmpty ? 0 : st.Amount;
            pop.SetCount(amt, showWhenOne: false);
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = !locked;
            canvasGroup.interactable = !locked;
        }
    }

    public bool IsLocked => isLocked;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (!isInitialized || Stack.IsEmpty) return;

        dragIcon = new GameObject("DragIcon", typeof(RectTransform)).GetComponent<RectTransform>();
        dragIcon.SetParent(rootCanvas.transform, false);
        dragIcon.SetAsLastSibling();

        dragIconImage = dragIcon.gameObject.AddComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.sprite = Stack.Item.Icon;
        dragIconImage.preserveAspect = true;
        dragIcon.sizeDelta = new Vector2(64f, 64f);

        var cg = dragIcon.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        int previewQty = ComputeDragQuantityPreview(Stack.Amount);
        dragCountTMP = null;
        dragCountTextLegacy = null;

        if (previewQty > 1)
        {
            var countGO = new GameObject("Count", typeof(RectTransform));
            countGO.transform.SetParent(dragIcon, false);

            var rt = countGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-4f, 4f);
            rt.sizeDelta = Vector2.zero;

            dragCountTMP = countGO.AddComponent<TextMeshProUGUI>();
            if (dragCountTMP != null)
            {
                dragCountTMP.text = previewQty.ToString();
                dragCountTMP.alignment = TextAlignmentOptions.BottomRight;
                dragCountTMP.fontSize = 20f;
                dragCountTMP.raycastTarget = false;

                var outline = countGO.AddComponent<Outline>();
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else
            {
                dragCountTextLegacy = countGO.AddComponent<Text>();
                dragCountTextLegacy.text = previewQty.ToString();
                dragCountTextLegacy.alignment = TextAnchor.LowerRight;
                dragCountTextLegacy.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                dragCountTextLegacy.raycastTarget = false;
            }
        }

        if (canvasGroup) canvasGroup.blocksRaycasts = false;
        MoveDragIcon(eventData);

        DragContext.Begin(Stack.Item, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (dragIcon != null) MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        if (dragIcon != null) Destroy(dragIcon.gameObject);
        dragIcon = null;
        dragCountTMP = null;
        dragCountTextLegacy = null;

        DragContext.End();
        HideAllDropHints();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isLocked) { HideAllDropHints(); return; }
        if (!isInitialized) { HideAllDropHints(); return; }

        var source = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<ItemSlotUI>() : null;
        if (source == null || source == this) { HideAllDropHints(); return; }

        var from = source.Stack;
        var to = Stack;
        if (from.IsEmpty) { HideAllDropHints(); return; }

        int qty = ComputeDropQuantity(from.Amount, source.owner, owner);

        if (to.IsEmpty || to.Item == from.Item)
        {
            Inventory.MoveQuantity(from, to, qty);
            source.Refresh();
            Refresh();
            NotifyOwnerChanged(source);
            NotifyOwnerChanged(this);
            HideAllDropHints();
            return;
        }

        bool movingFullStack = qty >= from.Amount;
        bool bothInventory = IsInventoryOwner(source.owner) && IsInventoryOwner(owner);

        if (movingFullStack && bothInventory)
        {
            Inventory.Swap(to, from);
            source.Refresh();
            Refresh();
            NotifyOwnerChanged(source);
            NotifyOwnerChanged(this);
            HideAllDropHints();
            return;
        }

        HideAllDropHints();
    }

    int ComputeDropQuantity(int available, OwnerType fromOwner, OwnerType toOwner)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                 || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (shift) return available;
        if (ctrl) return Mathf.Max(1, Mathf.CeilToInt(available / 2f));
        if (alt) return 1;

        bool fromStorage = IsInventoryOwner(fromOwner);
        bool toCrafting = toOwner == OwnerType.Crafting;

        if (fromStorage && toCrafting)
            return 1;

        return available;
    }

    int ComputeDragQuantityPreview(int available)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                 || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (shift) return available;
        if (ctrl) return Mathf.Max(1, Mathf.CeilToInt(available / 2f));
        if (alt) return 1;

        return IsInventoryOwner(owner) ? 1 : available;
    }

    void HideAllDropHints()
    {
        var hints = Object.FindObjectsByType<SlotHoverDropHint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hints.Length; i++)
        {
            var hint = hints[i];
            if (hint) hint.HideNow();
        }
    }

    void MoveDragIcon(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragIcon.position = eventData.position;
        }
        else if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                     (RectTransform)rootCanvas.transform, eventData.position,
                     eventData.pressEventCamera, out var local))
        {
            dragIcon.localPosition = local;
        }
    }

    static void NotifyOwnerChanged(ItemSlotUI slot)
    {
        if (slot == null) return;
        if (IsInventoryOwner(slot.owner))
            slot.inventory?.RaiseChanged();
        else if (slot.owner == OwnerType.Crafting)
            slot.craftingGrid?.RaiseChanged();
    }
}
