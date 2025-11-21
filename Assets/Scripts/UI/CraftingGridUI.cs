using UnityEngine;
using UnityEngine.UI;

public class CraftingGridUI : MonoBehaviour
{
    public CraftingGrid grid;
    public GameObject slotPrefab; // das gleiche Slot-Prefab
    public Transform slotParent;  // GridLayoutGroup (4x4)

    private ItemSlotUI[] slots;

    void OnEnable()
    {
        if (grid != null) grid.OnChanged += RefreshAll;
        if (slots == null) Build();
        else RefreshAll();
    }

    void OnDisable()
    {
        if (grid != null) grid.OnChanged -= RefreshAll;
    }

public void Build()
{
    if (grid == null) { Debug.LogError("[CraftingGridUI] 'grid' ist nicht zugewiesen.", this); return; }
    if (slotPrefab == null) { Debug.LogError("[CraftingGridUI] 'slotPrefab' ist nicht zugewiesen.", this); return; }
    if (slotParent == null) { Debug.LogError("[CraftingGridUI] 'slotParent' ist nicht zugewiesen.", this); return; }

    foreach (Transform child in slotParent) Destroy(child.gameObject);

    int total = CraftingGrid.Width * CraftingGrid.Height;
    slots = new ItemSlotUI[total];

    for (int i = 0; i < total; i++)
    {
        var go = Instantiate(slotPrefab, slotParent);
        var ui = go.GetComponent<ItemSlotUI>();
        if (ui == null) { Debug.LogError("[CraftingGridUI] slotPrefab hat kein ItemSlotUI!", slotPrefab); Destroy(go); continue; }

        ui.Init(grid, i);
        slots[i] = ui;
    }
}


    public void RefreshAll()
    {
        if (slots == null)
        {
            Build();
            return;
        }
        foreach (var s in slots) if (s) s.Refresh();
    }
}
