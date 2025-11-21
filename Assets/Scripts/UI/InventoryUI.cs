using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    [System.NonSerialized]
    public Inventory inventory;          // wird via SetInventory oder autoService gesetzt
    [SerializeField] GlobalInventoryService autoInventoryService;
    public Transform slotParent;         // -> BackpackGrid
    public GameObject slotPrefab;        // -> Slot prefab mit ItemSlotUI

    [Header("Slot Identity")]
    public ItemSlotUI.OwnerType ownerType = ItemSlotUI.OwnerType.Inventory;

    ItemSlotUI[] slots;
    CanvasGroup interactionCg;
    public System.Action<InventoryUI, Inventory> OnInventoryBound;
    public Inventory CurrentInventory => inventory;

    void Awake()
    {
        TryAutoBind();
        interactionCg = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        TryAutoBind();
        if (inventory != null) inventory.OnChanged += RefreshAll;
        if (slots == null || slots.Length == 0)
        {
            Build();
        }
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= RefreshAll;
        }
    }

    public void SetInventory(Inventory newInv)
    {
        if (inventory == newInv) return;
        if (inventory != null) inventory.OnChanged -= RefreshAll;
        inventory = newInv;
        if (inventory != null) inventory.OnChanged += RefreshAll;
        Build();
        PropagateInventoryToEmptyStates();
        OnInventoryBound?.Invoke(this, inventory);
    }

    public void SetAutoInventoryService(GlobalInventoryService service)
    {
        autoInventoryService = service;
        TryAutoBind();
    }

    void TryAutoBind()
    {
        if (inventory != null) return;
        if (autoInventoryService != null && autoInventoryService.playerInventory != null)
        {
            SetInventory(autoInventoryService.playerInventory);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (interactionCg == null) interactionCg = gameObject.GetComponent<CanvasGroup>();
        if (interactionCg == null) interactionCg = gameObject.AddComponent<CanvasGroup>();
        interactionCg.interactable = interactable;
        interactionCg.blocksRaycasts = interactable;
        interactionCg.alpha = interactable ? 1f : 0.7f;
    }

    public void Build()
    {
        if (!slotParent || !slotPrefab || inventory == null || inventory.Slots == null)
        {
            return;
        }

        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);

        int count = inventory.Slots.Count;
        slots = new ItemSlotUI[count];

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(slotPrefab, slotParent);
            var ui = go.GetComponent<ItemSlotUI>();
            if (!ui)
            {
                Debug.LogWarning("[InventoryUI] slotPrefab missing ItemSlotUI component.", this);
                Destroy(go);
                continue;
            }
            ui.Init(inventory, i, ownerType);
            slots[i] = ui;
        }

        ForceLayoutNow();
        PropagateInventoryToEmptyStates();
    }

    public void RefreshAll()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].Refresh();

        ForceLayoutNow();
    }

    void ForceLayoutNow()
    {
        var rt = slotParent as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    public ItemSlotUI[] GetSlotUIs() => slots;
    public ItemSlotUI[] Slots => slots;

    void PropagateInventoryToEmptyStates()
    {
        var states = GetComponentsInChildren<InventoryEmptyState>(true);
        foreach (var state in states)
            if (state) state.SetInventory(inventory);
    }
}
