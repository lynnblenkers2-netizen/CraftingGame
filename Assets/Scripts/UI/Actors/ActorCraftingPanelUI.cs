using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Actor-specific crafting panel. Uses the actor's backpack as inventory and a dedicated crafting grid.
/// Only allows recipes the player already knows via RecipeCatalogService.
/// </summary>
public class ActorCraftingPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private CraftingGrid grid;
    [SerializeField] private CraftingGridUI gridUI;
    [SerializeField] private InventoryUI backpackUI;
    [SerializeField] private Button craftOnceButton;
    [SerializeField] private Button autoCraftButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI recipeLabel;
    [SerializeField] private TextMeshProUGUI craftedAmountLabel;

    [Header("Autocraft")]
    [Min(0.2f)] [SerializeField] private float autoCraftSeconds = 1.5f;
    [SerializeField] private string autoCraftButtonLabel = "Autocraft";
    [SerializeField] private string stopAutoCraftButtonLabel = "Stop";

    [Header("Messages")]
    [SerializeField] private string msgRecipeUnknown = "Recipe not known.";
    [SerializeField] private string msgNoRecipeKnown = "No recipe known yet.";
    [SerializeField] private string msgArrangeKnownRecipe = "Arrange a learned recipe in the grid.";
    [SerializeField] private string msgMissingItems = "Not enough materials in backpack.";
    [SerializeField] private string msgBackpackFull = "No space left in backpack.";
    [SerializeField] private string msgAutocraftStopped = "Autocraft stopped: {0}";
    [SerializeField] private string msgCrafted = "Crafted {0}x {1}.";

    ActorInstance currentActor;
    RecipeCatalogService catalog;
    Coroutine autoRoutine;
    Coroutine craftOnceRoutine;
    AutoCraftVisual activeVisual;
    readonly Dictionary<string, ItemStack[]> savedGrids = new();
    Inventory subscribedBackpack;

    class AutoCraftVisual
    {
        public int slotIndex = -1;
        public ItemSlotUI slotUI;
        public Image background;
        public Image fill;

        public void SetProgress(float p)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(p);
        }

        public void Dispose()
        {
            if (slotUI != null) slotUI.SetLocked(false);
            if (background != null) Object.Destroy(background.gameObject);
        }
    }

    void Awake()
    {
        catalog = RecipeCatalogService.Instance ?? FindObjectOfType<RecipeCatalogService>();
    }

    void OnEnable()
    {
        WireButtons();
        SubscribeEvents();
        RefreshPanel();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
        StopAutocraft(string.Empty);
        SaveGridState();
    }

    public void Open(ActorInstance actor)
    {
        SaveGridState();
        UnsubscribeEvents();
        currentActor = actor;
        if (root) root.SetActive(true);
        if (backpackUI != null && actor != null)
        {
            backpackUI.ownerType = ItemSlotUI.OwnerType.Backpack;
            backpackUI.SetInventory(actor.backpack);
            backpackUI.Build();
        }
        SubscribeEvents();
        RestoreGridState(actor);
        RefreshPanel();
    }

    public void Close()
    {
        StopAutocraft(string.Empty);
        SaveGridState();
        if (root) root.SetActive(false);
    }

    void WireButtons()
    {
        if (craftOnceButton)
        {
            craftOnceButton.onClick.RemoveAllListeners();
            craftOnceButton.onClick.AddListener(CraftOnce);
        }

        if (autoCraftButton)
        {
            autoCraftButton.onClick.RemoveAllListeners();
            autoCraftButton.onClick.AddListener(ToggleAutocraft);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    void SubscribeEvents()
    {
        if (grid != null) grid.OnChanged += RefreshPanel;
        if (currentActor != null && currentActor.backpack != null)
        {
            subscribedBackpack = currentActor.backpack;
            subscribedBackpack.OnChanged += RefreshPanel;
        }
    }

    void UnsubscribeEvents()
    {
        if (grid != null) grid.OnChanged -= RefreshPanel;
        if (subscribedBackpack != null)
        {
            subscribedBackpack.OnChanged -= RefreshPanel;
            subscribedBackpack = null;
        }
    }

    void RefreshPanel()
    {
        if (gridUI != null && grid != null && gridUI.gameObject.activeInHierarchy)
            gridUI.RefreshAll();

        UpdateRecipeLabel();
        UpdateButtonState();
    }

    void UpdateRecipeLabel()
    {
        if (recipeLabel == null)
            return;

        if (!HasAnyKnownRecipe())
        {
            recipeLabel.text = msgNoRecipeKnown;
            SetCraftedAmountText(string.Empty);
            return;
        }

        if (TryFindMatchingRecipe(out var recipe, out _))
        {
            string name = !string.IsNullOrEmpty(recipe.displayName) ? recipe.displayName : recipe.name;
            recipeLabel.text = name;
        }
        else
        {
            recipeLabel.text = msgArrangeKnownRecipe;
        }
    }

    void UpdateButtonState()
    {
        bool hasActor = currentActor != null && currentActor.backpack != null;
        bool hasRecipe = TryFindMatchingRecipe(out _, out _);
        bool busy = autoRoutine != null || craftOnceRoutine != null;
        if (craftOnceButton) craftOnceButton.interactable = hasActor && hasRecipe && !busy;
        if (autoCraftButton)
        {
            autoCraftButton.interactable = hasActor && hasRecipe && craftOnceRoutine == null;
            autoCraftButton.GetComponentInChildren<TextMeshProUGUI>()?.SetText(autoRoutine == null
                ? autoCraftButtonLabel
                : stopAutoCraftButtonLabel);
        }
    }

    void CraftOnce()
    {
        if (autoRoutine != null || craftOnceRoutine != null) return; // avoid double-running
        if (currentActor == null || currentActor.backpack == null) return;

        if (!TryFindMatchingRecipe(out var recipe, out var needs))
        {
            ToastSystem.Warning(msgRecipeUnknown, msgArrangeKnownRecipe);
            return;
        }

        if (!HasIngredients(recipe, needs))
        {
            ToastSystem.Warning(msgMissingItems, recipe.name);
            return;
        }

        craftOnceRoutine = StartCoroutine(RunCraftOnce(recipe, needs));
        UpdateButtonState();
    }

    void ToggleAutocraft()
    {
        if (autoRoutine != null)
        {
            StopAutocraft("stopped manually");
            return;
        }

        if (currentActor == null || currentActor.backpack == null) return;
        if (!TryFindMatchingRecipe(out var recipe, out var needs))
        {
            ToastSystem.Warning(msgRecipeUnknown, msgArrangeKnownRecipe);
            return;
        }

        if (!HasIngredients(recipe, needs))
        {
            ToastSystem.Warning(msgMissingItems, recipe.name);
            return;
        }

        autoRoutine = StartCoroutine(RunAutocraft(recipe, needs));
        SetActorCraftingState(true, $"Started autocrafting {recipe.name}.");
        if (autoCraftButton != null)
            autoCraftButton.GetComponentInChildren<TextMeshProUGUI>()?.SetText(stopAutoCraftButtonLabel);
    }

    IEnumerator RunAutocraft(ShapedRecipe recipe, Dictionary<Item, int> needs)
    {
        while (true)
        {
            if (currentActor == null || currentActor.backpack == null)
            {
                StopAutocraft("no actor");
                yield break;
            }

            if (!GridMatchesRecipe(recipe))
            {
                StopAutocraft("grid no longer matches recipe");
                yield break;
            }

            if (!HasIngredients(recipe, needs))
            {
                StopAutocraft("missing materials");
                yield break;
            }

            var visual = EnsureAutoCraftVisual(recipe.outputItem);
            if (visual == null)
            {
                StopAutocraft(msgBackpackFull);
                yield break;
            }

            float t = 0f;
            while (t < autoCraftSeconds)
            {
                t += Time.deltaTime;
                visual.SetProgress(t / autoCraftSeconds);
                yield return null;
            }

            if (!ConsumeIngredients(recipe, needs))
            {
                StopAutocraft("missing materials");
                yield break;
            }

            if (!AddOutput(recipe))
            {
                StopAutocraft(msgBackpackFull);
                yield break;
            }

            currentActor.AddStatement($"Autocrafted {recipe.name}.");

            if (visual != null) visual.SetProgress(0f);
            var craftedText = string.Format(msgCrafted, recipe.outputAmount, recipe.outputItem.DisplayName);
            SetCraftedAmountText(craftedText);
            RefreshPanel();
            yield return null;
        }
    }

    void StopAutocraft(string reason)
    {
        bool wasRunning = autoRoutine != null;
        if (wasRunning)
            StopCoroutine(autoRoutine);
        autoRoutine = null;
        DisposeVisual();
        if (!string.IsNullOrEmpty(reason))
            ToastSystem.Info("Autocraft", string.Format(msgAutocraftStopped, reason));
        if (wasRunning)
            SetActorCraftingState(false, string.IsNullOrEmpty(reason) ? null : reason);
        UpdateButtonState();
    }

    IEnumerator RunCraftOnce(ShapedRecipe recipe, Dictionary<Item, int> needs)
    {
        var visual = EnsureAutoCraftVisual(recipe.outputItem);
        if (visual == null)
        {
            ToastSystem.Warning(msgBackpackFull, recipe.name);
            craftOnceRoutine = null;
            UpdateButtonState();
            yield break;
        }

        float t = 0f;
        while (t < autoCraftSeconds)
        {
            t += Time.deltaTime;
            visual.SetProgress(t / autoCraftSeconds);
            yield return null;
        }

        if (!ConsumeIngredients(recipe, needs))
        {
            ToastSystem.Warning(msgMissingItems, recipe.name);
            craftOnceRoutine = null;
            UpdateButtonState();
            yield break;
        }

        if (!AddOutput(recipe))
        {
            ToastSystem.Warning(msgBackpackFull, recipe.name);
            craftOnceRoutine = null;
            UpdateButtonState();
            yield break;
        }

        currentActor.AddStatement($"Crafted {recipe.name}.");
        var craftedText = string.Format(msgCrafted, recipe.outputAmount, recipe.outputItem.DisplayName);
        ToastSystem.Success("Crafted", craftedText);
        SetCraftedAmountText(craftedText);
        RefreshPanel();

        craftOnceRoutine = null;
        DisposeVisual();
        UpdateButtonState();
    }

    AutoCraftVisual EnsureAutoCraftVisual(Item output)
    {
        if (backpackUI == null || currentActor == null || output == null) return null;
        if (activeVisual != null && activeVisual.slotUI != null) return activeVisual;

        int slotIndex = FindSlotForOutput(output);
        if (slotIndex < 0) return null;

        var slotUI = FindSlotUI(slotIndex);
        if (slotUI == null) return null;

        slotUI.SetLocked(true);

        var holder = new GameObject("AutoCraftGhost", typeof(RectTransform));
        holder.transform.SetParent(slotUI.transform, false);
        var bg = holder.AddComponent<Image>();
        bg.sprite = output.Icon;
        bg.color = new Color(1f, 1f, 1f, 0.25f);
        bg.raycastTarget = false;

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(holder.transform, false);
        var fill = fillGO.AddComponent<Image>();
        fill.sprite = output.Icon;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        activeVisual = new AutoCraftVisual
        {
            slotIndex = slotIndex,
            slotUI = slotUI,
            background = bg,
            fill = fill
        };
        return activeVisual;
    }

    void DisposeVisual()
    {
        if (activeVisual != null)
        {
            activeVisual.Dispose();
            activeVisual = null;
        }
    }

    ItemSlotUI FindSlotUI(int index)
    {
        if (backpackUI == null) return null;
        var slots = backpackUI.GetSlotUIs();
        if (slots == null || index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    int FindSlotForOutput(Item output)
    {
        var inv = currentActor?.backpack;
        if (inv == null) return -1;

        for (int i = 0; i < inv.Slots.Count; i++)
        {
            var st = inv.GetSlot(i);
            if (!st.IsEmpty && st.Item == output && st.Amount < st.Item.MaxStack)
                return i;
        }

        for (int i = 0; i < inv.Slots.Count; i++)
        {
            var st = inv.GetSlot(i);
            if (st.IsEmpty) return i;
        }
        return -1;
    }

    bool TryFindMatchingRecipe(out ShapedRecipe recipe, out Dictionary<Item, int> needs)
    {
        recipe = null;
        needs = null;
        if (grid == null) return false;

        if (catalog == null)
            catalog = RecipeCatalogService.Instance ?? FindObjectOfType<RecipeCatalogService>();

        var known = catalog != null ? catalog.GetAll() : null;
        if (known == null || known.Count == 0) return false;

        foreach (var r in known)
        {
            if (r == null || r.pattern == null || r.pattern.Length != CraftingGrid.Width * CraftingGrid.Height) continue;
            if (ComputeNeedsIfShapeMatches(grid, r, out needs))
            {
                recipe = r;
                return true;
            }
        }
        recipe = null;
        needs = null;
        return false;
    }

    bool HasAnyKnownRecipe()
    {
        if (catalog == null)
            catalog = RecipeCatalogService.Instance ?? FindObjectOfType<RecipeCatalogService>();
        var known = catalog != null ? catalog.GetAll() : null;
        return known != null && known.Count > 0;
    }

    bool GridMatchesRecipe(ShapedRecipe recipe)
    {
        if (recipe == null || grid == null) return false;
        return ComputeNeedsIfShapeMatches(grid, recipe, out _);
    }

    static bool ComputeNeedsIfShapeMatches(CraftingGrid grid, ShapedRecipe recipe, out Dictionary<Item, int> needsPerItem)
    {
        needsPerItem = new Dictionary<Item, int>();
        for (int i = 0; i < CraftingGrid.Width * CraftingGrid.Height; i++)
        {
            var req = recipe.pattern[i];
            var cell = grid.GetCell(i);
            bool reqEmpty = req.item == null || req.amount <= 0;
            if (reqEmpty)
            {
                if (!cell.IsEmpty) return false;
            }
            else
            {
                if (cell.IsEmpty || cell.Item != req.item) return false;
                if (!needsPerItem.ContainsKey(req.item)) needsPerItem[req.item] = 0;
                needsPerItem[req.item] += Mathf.Max(1, req.amount);
            }
        }
        return true;
    }

    bool HasIngredients(ShapedRecipe recipe, Dictionary<Item, int> needs)
    {
        if (recipe == null || needs == null || currentActor == null || currentActor.backpack == null) return false;
        foreach (var kv in needs)
        {
            int available = CountInBackpack(kv.Key) + CountInGrid(kv.Key);
            if (available < kv.Value) return false;
        }
        return true;
    }

    int CountInBackpack(Item item)
    {
        return currentActor?.backpack != null ? currentActor.backpack.GetTotalAmount(item) : 0;
    }

    int CountInGrid(Item item)
    {
        if (grid == null || item == null) return 0;
        int total = 0;
        for (int i = 0; i < CraftingGrid.Width * CraftingGrid.Height; i++)
        {
            var cell = grid.GetCell(i);
            if (!cell.IsEmpty && cell.Item == item) total += cell.Amount;
        }
        return total;
    }

    bool ConsumeIngredients(ShapedRecipe recipe, Dictionary<Item, int> needs)
    {
        if (recipe == null || needs == null || currentActor == null || currentActor.backpack == null) return false;

        var remaining = new Dictionary<Item, int>(needs);
        // Prefer backpack to keep the grid layout intact as long as possible
        foreach (var kv in needs)
        {
            int pulled = currentActor.backpack.RemoveUpTo(kv.Key, kv.Value);
            remaining[kv.Key] = Mathf.Max(0, kv.Value - pulled);
        }

        for (int i = 0; i < CraftingGrid.Width * CraftingGrid.Height; i++)
        {
            var req = recipe.pattern[i];
            if (req.item == null || req.amount <= 0) continue;

            var cell = grid.GetCell(i);
            if (cell.IsEmpty || cell.Item != req.item) continue;

            int need = remaining[req.item];
            if (need <= 0) continue;

            int take = Mathf.Min(need, cell.Amount);
            cell.Amount -= take;
            remaining[req.item] -= take;
            if (cell.Amount <= 0) cell.Clear();
        }

        foreach (var kv in remaining)
        {
            if (kv.Value > 0)
            {
                grid.RaiseChanged();
                currentActor.backpack.RaiseChanged();
                return false;
            }
        }

        grid.RaiseChanged();
        currentActor.backpack.RaiseChanged();
        return true;
    }

    bool AddOutput(ShapedRecipe recipe)
    {
        if (recipe == null || currentActor == null || currentActor.backpack == null) return false;
        int left = currentActor.backpack.Add(recipe.outputItem, recipe.outputAmount);
        currentActor.backpack.RaiseChanged();
        return left <= 0;
    }

    void SaveGridState()
    {
        if (currentActor == null || grid == null) return;
        var snap = new ItemStack[CraftingGrid.Width * CraftingGrid.Height];
        for (int i = 0; i < snap.Length; i++)
        {
            var cell = grid.GetCell(i);
            snap[i] = new ItemStack(cell.Item, cell.Amount);
        }
        savedGrids[currentActor.instanceId] = snap;
    }

    void RestoreGridState(ActorInstance actor)
    {
        if (grid == null || actor == null) return;
        if (!savedGrids.TryGetValue(actor.instanceId, out var snap) || snap == null)
        {
            grid.Clear();
            grid.RaiseChanged();
            gridUI?.RefreshAll();
            return;
        }

        int len = Mathf.Min(snap.Length, CraftingGrid.Width * CraftingGrid.Height);
        for (int i = 0; i < len; i++)
        {
            var cell = grid.GetCell(i);
            var src = snap[i];
            if (src != null)
            {
                cell.Item = src.Item;
                cell.Amount = src.Amount;
            }
            else
            {
                cell.Clear();
            }
        }
        grid.RaiseChanged();
        gridUI?.RefreshAll();
    }

    void SetActorCraftingState(bool working, string statement)
    {
        if (currentActor == null) return;
        currentActor.state = working ? ActorState.Working : ActorState.Idle;
        currentActor.taskType = working ? "craft" : null;
        currentActor.taskAsset = working ? null : null;
        currentActor.remainingDays = 0f;
        if (!string.IsNullOrEmpty(statement))
            currentActor.AddStatement(statement);
    }

    void SetCraftedAmountText(string text)
    {
        if (craftedAmountLabel == null) return;
        craftedAmountLabel.text = text ?? string.Empty;
    }
}
