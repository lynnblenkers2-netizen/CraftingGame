using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public Item Item;
    public int Amount;

    public bool IsEmpty => Item == null || Amount <= 0;
    public int RemainingSpace => IsEmpty ? 0 : Mathf.Max(0, Item.MaxStack - Amount);

    public ItemStack() { }
    public ItemStack(Item item, int amount) { Item = item; Amount = amount; }

    public static ItemStack Empty() => new ItemStack();

    public void Clear() { Item = null; Amount = 0; }
}
