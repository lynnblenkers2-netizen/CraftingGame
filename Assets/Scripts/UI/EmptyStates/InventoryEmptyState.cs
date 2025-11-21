using UnityEngine;
using TMPro;

public class InventoryEmptyState : MonoBehaviour
{
    Inventory inventory;
    [Header("Wiring")]
    public GameObject emptyPanel;          // Empty_Inventory
    public TextMeshProUGUI label;          // optional
    [SerializeField] bool useFallbackInventory = false;
    [SerializeField] Inventory fallbackInventory;
    [SerializeField] InventoryUI inventoryUIReference;
    [SerializeField] GlobalInventoryService autoInventoryService;
    [SerializeField] bool autoBindFromParentUI = true;
    [SerializeField, TextArea]
    private string emptyMessage = "No items yet.\nFind or craft materials.";
    [SerializeField] bool retryUntilBound = true;

    InventoryUI subscribedUI;

    void Awake() => TryAutoBind();

    void OnEnable()
    {
        TryAutoBind();
        Subscribe();
        Refresh();
    }
    void OnDisable()
    {
        Unsubscribe();
        DetachUIListener();
    }

    void Update()
    {
        if (!retryUntilBound || inventory != null) return;
        TryAutoBind();
    }

    void Subscribe()
    {
        if (inventory != null)
            inventory.OnChanged += Refresh;
    }

    void Unsubscribe()
    {
        if (inventory != null)
            inventory.OnChanged -= Refresh;
    }

    public void SetInventory(Inventory newInv)
    {
        if (inventory == newInv) return;
        Unsubscribe();
        inventory = newInv;
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    void TryAutoBind()
    {
        if (inventory != null) return;

        if (autoBindFromParentUI)
        {
            var ui = inventoryUIReference != null ? inventoryUIReference : GetComponentInParent<InventoryUI>();
            if (ui != null)
            {
                AttachUIListener(ui);
                if (ui.CurrentInventory != null)
                {
                    SetInventory(ui.CurrentInventory);
                    return;
                }
                return;
            }
        }

        if (autoInventoryService != null && autoInventoryService.playerInventory != null)
        {
            SetInventory(autoInventoryService.playerInventory);
            return;
        }

        if (useFallbackInventory && fallbackInventory != null)
        {
            SetInventory(fallbackInventory);
            return;
        }
    }

    void AttachUIListener(InventoryUI ui)
    {
        if (ui == null) return;
        if (subscribedUI == ui) return;
        DetachUIListener();
        subscribedUI = ui;
        subscribedUI.OnInventoryBound += HandleInventoryBound;
    }

    void DetachUIListener()
    {
        if (subscribedUI != null)
        {
            subscribedUI.OnInventoryBound -= HandleInventoryBound;
            subscribedUI = null;
        }
    }

    void HandleInventoryBound(InventoryUI ui, Inventory inv)
    {
        if (ui != subscribedUI) return;
        SetInventory(inv);
    }

    void Refresh()
    {
        bool any = false;
        if (inventory != null)
        {
            var slots = inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty) { any = true; break; }
        }
        if (label) label.text = emptyMessage;
        if (emptyPanel)
        {
            var fade = emptyPanel.GetComponent<FadeToggle>();
            if (fade) fade.SetVisible(!any);
            else
            {
                bool prev = emptyPanel.activeSelf;
                bool now = !any;
                emptyPanel.SetActive(now);
            }
        }
    }
}
