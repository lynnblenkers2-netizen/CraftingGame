using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [SerializeField] private GlobalInventoryService inventoryService;
    [SerializeField] private CraftingGrid grid;
    [SerializeField] private List<ShapedRecipe> recipes = new();
    [Header("Editor Notes")]
    [TextArea(3,6)]
    [SerializeField] public string editorNote = "This CraftingManager holds the recipes this manager exposes.\nIf you want a single, project-wide recipe list create one via Tools/Recipes/Build Recipe Database.\nDiscovered recipes are propagated into CraftingManager at runtime by RecipeCatalogService.";

    // Expose the configured recipes so other services can seed from them
    public IReadOnlyList<ShapedRecipe> Recipes => recipes;

    /// <summary>
    /// Add a recipe to this manager at runtime if not already present.
    /// This allows discovered recipes to become craftable without modifying assets.
    /// </summary>
    public void AddRuntimeRecipe(ShapedRecipe r)
    {
        if (r == null) return;
        if (recipes == null) recipes = new List<ShapedRecipe>();
        if (!recipes.Contains(r))
        {
            recipes.Add(r);
            Debug.Log($"CraftingManager.AddRuntimeRecipe: added recipe {r.name} (now {recipes.Count} recipes)");
            // Try to refresh any UI bound to this manager
            ForceUIRefresh();
        }
        else
        {
            Debug.Log($"CraftingManager.AddRuntimeRecipe: recipe {r.name} already present");
        }
    }
    
    // Optional: Direkte UI-Referenzen, um bei jeder Änderung hart zu refreshen
    // (zusätzlich zu den Events der Datenobjekte)
    [Header("UI (optional)")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftingGridUI craftingGridUI;
    [Header("XP Reward")]
    [SerializeField] private int xpPerCraft = 1;

    enum CraftAttemptResult { Success, NoRecipe, InventoryFull, Error }

    Inventory PlayerInventory => inventoryService != null ? inventoryService.playerInventory : null;
    [Header("Player Stats")]
    [SerializeField] private PlayerProgress playerProgress;

    private void OnEnable()
    {
        if (playerProgress == null) playerProgress = FindObjectOfType<PlayerProgress>();
        var inv = PlayerInventory;
        if (inv != null)
        {
            inv.OnChanged += ForceUIRefresh;
            if (inventoryUI != null) inventoryUI.SetInventory(inv);
        }
        if (grid != null) grid.OnChanged += ForceUIRefresh;
    }

    private void OnDisable()
    {
        var inv = PlayerInventory;
        if (inv != null) inv.OnChanged -= ForceUIRefresh;
        if (grid != null) grid.OnChanged -= ForceUIRefresh;
    }

    private void ForceUIRefresh()
    {
        // Falls UI-Referenzen gesetzt sind, erzwinge ein komplettes Refresh
        if (inventoryUI != null) inventoryUI.RefreshAll();
        if (craftingGridUI != null) craftingGridUI.RefreshAll();
    }

    // Button-Handler (OnClick -> CraftingManager.Craft)
    public void Craft()
    {
        var result = TryCraftOnce(out Item craftedItem, out int craftedAmount);
        switch (result)
        {
            case CraftAttemptResult.Success:
                Debug.Log("Crafting erfolgreich!");
                var btn = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                if (btn)
                {
                    var juice = btn.GetComponent<ButtonJuice>();
                    if (juice) juice.PulseSuccess();
                }
                {
                    string name = craftedItem ? craftedItem.DisplayName : "Item";
                    ToastSystem.Success("Gecraftet!", $"{craftedAmount}× {name}", key:"craft_ok");
                }
                break;
            case CraftAttemptResult.InventoryFull:
                ToastSystem.Error("Kein Platz im Inventar", "Räume einen Slot frei.", key:"inv_full");
                Debug.Log("[CraftingManager] Kein Platz im Inventar.");
                break;
            case CraftAttemptResult.NoRecipe:
                ToastSystem.Warning("Unbekanntes Rezept", "Kombination ergibt nichts.", key:"craft_unknown");
                Debug.Log("[CraftingManager] Kein passendes Rezept.");
                break;
            default:
                Debug.Log("[CraftingManager] Crafting fehlgeschlagen.");
                break;
        }

        // Sicherheitshalber UI immer refreshen (auch wenn TryCraftOnce intern Events raised)
        ForceUIRefresh();
    }

    private CraftAttemptResult TryCraftOnce(out Item craftedItem, out int craftedAmount)
    {
        craftedItem = null;
        craftedAmount = 0;
        var inv = PlayerInventory;
        if (inv == null || !grid || recipes == null || recipes.Count == 0) return CraftAttemptResult.Error;

        bool matchedRecipe = false;
        bool blockedBySpace = false;

        foreach (var r in recipes)
        {
            if (r == null || r.pattern == null || r.pattern.Length != 16) continue;

            // 1) Form + Bedarf ermitteln (shaped; Typen müssen passen, Menge egal; Bedarf je Item summieren)
            if (!ComputeNeedsIfShapeMatches(r, out var needsPerItem)) continue;
            matchedRecipe = true;

            // 2) SIMULATION auf Kopien von Inventar + Grid
            // --- Inventar kopieren
            var invSlots = inv.Slots;
            int invCount = invSlots.Count;
            var invItems = new Item[invCount];
            var invAmts  = new int[invCount];
            int emptyNow = 0;
            for (int i = 0; i < invCount; i++)
            {
                invItems[i] = invSlots[i].Item;
                invAmts[i]  = invSlots[i].Amount;
                if (invItems[i] == null || invAmts[i] <= 0) emptyNow++;
            }

            // --- Grid kopieren (nur Mengen relevant)
            var gridItems = new Item[16];
            var gridAmts  = new int[16];
            for (int i = 0; i < 16; i++)
            {
                var c = grid.GetCell(i);
                gridItems[i] = c.Item;
                gridAmts[i]  = c.Amount;
            }

            // --- Verbrauch simulieren (Inventar bevorzugt, Rest aus den vom Rezept verwendeten Zellen)
            int freedSlotsByConsumption = 0;

            foreach (var kv in needsPerItem)
            {
                Item item = kv.Key;
                int need  = kv.Value;
                if (item == null || need <= 0) { freedSlotsByConsumption = 0; break; }

                // aus Inventar
                for (int i = 0; i < invCount && need > 0; i++)
                {
                    if (invItems[i] != item || invAmts[i] <= 0) continue;
                    int take = Mathf.Min(invAmts[i], need);
                    invAmts[i] -= take;
                    need -= take;
                    if (invAmts[i] <= 0)
                    {
                        invItems[i] = null;
                        invAmts[i]  = 0;
                        freedSlotsByConsumption++; // Slot wird frei
                    }
                }
                // aus passenden Grid-Zellen
                for (int i = 0; i < 16 && need > 0; i++)
                {
                    var req = r.pattern[i];
                    if (req.item != item || req.amount <= 0) continue;
                    if (gridItems[i] != item || gridAmts[i] <= 0) continue;

                    int take = Mathf.Min(gridAmts[i], need);
                    gridAmts[i] -= take;
                    need -= take;
                    if (gridAmts[i] <= 0)
                    {
                        gridItems[i] = null;
                        gridAmts[i]  = 0;
                    }
                }
                if (need > 0)
                {
                    // unerwartet nicht gedeckt -> Rezept überspringen
                    freedSlotsByConsumption = 0;
                    goto NextRecipe;
                }
            }

            // --- Output-Einräumbarkeit simulieren
            int existingSpace = 0;
            if (r.outputItem)
            {
                for (int i = 0; i < invCount; i++)
                    if (invItems[i] == r.outputItem && invAmts[i] > 0)
                        existingSpace += Mathf.Max(0, r.outputItem.MaxStack - invAmts[i]);
            }

            int outputForSpace = ComputeCraftOutputForSpace(r.outputAmount);
            int remainAfterExisting = Mathf.Max(0, outputForSpace - existingSpace);
            int newStacksNeeded = (r.outputItem && r.outputItem.MaxStack > 0)
                ? Mathf.CeilToInt(remainAfterExisting / (float)r.outputItem.MaxStack)
                : 0;

            if (newStacksNeeded > emptyNow + freedSlotsByConsumption)
            {
                // nicht genug Slots, selbst nach Verbrauch
                blockedBySpace = true;
                goto NextRecipe;
            }

            // 3) WIRKLICH anwenden (jetzt ohne Überraschungen)

            // 3a) Verbrauch real: erst Inventar, dann Grid
            foreach (var kv in needsPerItem)
            {
                Item item = kv.Key;
                int need  = kv.Value;

                int tookInv = inv.RemoveUpTo(item, need);
                int remaining = need - tookInv;

                for (int i = 0; i < 16 && remaining > 0; i++)
                {
                    var req = r.pattern[i];
                    if (req.item != item || req.amount <= 0) continue;

                    var cell = grid.GetCell(i);
                    if (cell.IsEmpty || cell.Item != item) continue;

                    int take = Mathf.Min(cell.Amount, remaining);
                    cell.Amount -= take;
                    remaining   -= take;
                    if (cell.Amount <= 0) cell.Clear();
                }

                if (remaining > 0)
                {
                    Debug.LogWarning($"[CraftingManager] Unerwartet: Bedarf {item?.name} nicht gedeckt.");
                    grid.RaiseChanged();
                    return CraftAttemptResult.Error;
                }
            }

            // 3b) Output einsortieren
            int craftedAmountThisRun = ApplyCraftingSkill(r.outputAmount);
            int remainder = inv.Add(r.outputItem, craftedAmountThisRun);
            if (remainder > 0)
            {
                Debug.LogWarning("[CraftingManager] Unerwartet kein Platz für Output nach erfolgreicher Simulation.");
                grid.RaiseChanged();
                return CraftAttemptResult.InventoryFull;
            }

            // 3c) Output-Highlight im UI (erstes passendes Inventar-Slot blinken) – nur auf UIs, die dieses Inventar nutzen
            InventoryUI targetUI = inventoryUI;
            if (targetUI == null)
            {
                var all = FindObjectsOfType<InventoryUI>(true);
                foreach (var ui in all)
                {
                    if (ui != null && ui.CurrentInventory == inv)
                    {
                        targetUI = ui;
                        break;
                    }
                }
            }
            if (targetUI != null && targetUI.Slots != null && targetUI.Slots.Length > 0)
            {
                for (int i = 0; i < inv.Slots.Count; i++)
                {
                    var st = inv.GetSlot(i);
                    if (!st.IsEmpty && st.Item == r.outputItem)
                    {
                        var slotUI = (i >= 0 && i < targetUI.Slots.Length) ? targetUI.Slots[i] : null;
                        if (slotUI != null)
                        {
                            var flash = slotUI.gameObject.GetComponent<SlotHighlightFlash>();
                            if (!flash) flash = slotUI.gameObject.AddComponent<SlotHighlightFlash>();
                            if (flash) flash.Flash();
                        }
                        break;
                    }
                }
            }

            // 3d) UI refresh (Inventory.Add feuert OnChanged; Grid manuell)
            grid.RaiseChanged();
            craftedItem = r.outputItem;
            craftedAmount = craftedAmountThisRun;
            if (xpPerCraft > 0 && playerProgress != null)
                playerProgress.GrantXP(xpPerCraft);
            return CraftAttemptResult.Success;

            // label für "continue outer foreach"
            NextRecipe: ;
        }

        if (blockedBySpace) return CraftAttemptResult.InventoryFull;
        return matchedRecipe ? CraftAttemptResult.Error : CraftAttemptResult.NoRecipe;
    }

    /// Ermittelt Bedarf je Item, wenn die Form passt (shaped).
    /// Belegte Rezeptfelder: Grid-Zelle muss selben Item-Typ haben (Menge egal).
    /// Leere Rezeptfelder: Grid-Zelle muss leer sein.
    private bool ComputeNeedsIfShapeMatches(ShapedRecipe r, out Dictionary<Item, int> needsPerItem)
    {
        needsPerItem = new Dictionary<Item, int>();

        for (int i = 0; i < 16; i++)
        {
            var req  = r.pattern[i];
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

    // ---- Skill helpers (player-wide crafting bonuses) ----
    int ComputeCraftOutputForSpace(int baseAmount)
    {
        GetCraftingBonuses(out var chance, out var multiplier);
        float potentialMultiplier = Mathf.Max(1f, multiplier);
        return Mathf.RoundToInt(baseAmount * potentialMultiplier);
    }

    int ApplyCraftingSkill(int baseAmount)
    {
        GetCraftingBonuses(out var chance, out var multiplier);
        if (chance <= 0f || multiplier <= 1f) return baseAmount;

        return (UnityEngine.Random.value < chance)
            ? Mathf.RoundToInt(baseAmount * multiplier)
            : baseAmount;
    }

    void GetCraftingBonuses(out float chance, out float multiplier)
    {
        chance = 0f;
        multiplier = 1f;
        if (playerProgress == null) return;
        playerProgress.GetCraftingBonuses(out chance, out multiplier);
    }
}
