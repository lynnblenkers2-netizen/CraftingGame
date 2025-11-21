using UnityEngine;

public class GlobalInventoryService : MonoBehaviour
{
    [Min(1)] public int startingCapacity = 24;
    public Inventory playerInventory { get; private set; }

    [Header("Optional Start Items")]
    public Item[] startItems;
    public int[] startAmounts;

    void Awake()
    {
        int capacity = Mathf.Max(1, startingCapacity);
        if (playerInventory == null) playerInventory = new Inventory(capacity);
        else playerInventory.SetCapacity(capacity);
        SeedStartItems();
    }

    void SeedStartItems()
    {
        if (startItems == null || startAmounts == null) return;
        int count = Mathf.Min(startItems.Length, startAmounts.Length);
        bool seeded = false;
        for (int i = 0; i < count; i++)
        {
            var item = startItems[i];
            int amount = i < startAmounts.Length ? startAmounts[i] : 0;
            if (!item || amount <= 0) continue;
            playerInventory.Add(item, amount, notify: false);
            seeded = true;
        }
        if (seeded) playerInventory.RaiseChanged();
    }
}
