using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory : ISerializationCallbackReceiver
{
    [SerializeField] List<ItemStack> slots = new();
    [SerializeField] int lastKnownCapacity = 1;

    public List<ItemStack> Slots
    {
        get
        {
            EnsureSlots();
            return slots;
        }
    }
    public event Action OnChanged;

    public int Capacity => (slots != null && slots.Count > 0) ? slots.Count : Mathf.Max(1, lastKnownCapacity);

    public Inventory() : this(1) { }

    public Inventory(int capacity)
    {
        InitSlots(capacity);
    }

    void InitSlots(int capacity)
    {
        capacity = Mathf.Max(1, capacity);
        lastKnownCapacity = capacity;
        slots = new List<ItemStack>(capacity);
        for (int i = 0; i < capacity; i++)
            slots.Add(ItemStack.Empty());
    }

    void EnsureSlots()
    {
        if (slots == null || slots.Count == 0)
        {
            InitSlots(lastKnownCapacity);
        }
        else
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] == null)
                    slots[i] = ItemStack.Empty();
            lastKnownCapacity = slots.Count;
        }
    }

    public void RaiseChanged() => NotifyChanged();

    void NotifyChanged() => OnChanged?.Invoke();

    public ItemStack GetSlot(int index)
    {
        EnsureSlots();
        return slots[index];
    }

    public void SetCapacity(int newCapacity)
    {
        EnsureSlots();
        newCapacity = Mathf.Max(1, newCapacity);
        if (slots.Count == newCapacity) { lastKnownCapacity = newCapacity; return; }

        if (slots.Count < newCapacity)
        {
            for (int i = slots.Count; i < newCapacity; i++) slots.Add(ItemStack.Empty());
        }
        else if (slots.Count > newCapacity)
        {
            for (int i = newCapacity; i < slots.Count; i++) slots[i].Clear();
            slots.RemoveRange(newCapacity, slots.Count - newCapacity);
        }
        lastKnownCapacity = newCapacity;
        NotifyChanged();
    }

    public int GetTotalAmount(Item item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var s in Slots) if (s.Item == item) total += s.Amount;
        return total;
    }

    public bool CanAdd(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        int remaining = amount;

        foreach (var st in Slots)
        {
            if (st.Item == item)
            {
                int addable = Mathf.Min(st.RemainingSpace, remaining);
                remaining -= addable;
                if (remaining <= 0) return true;
            }
        }

        foreach (var st in Slots)
        {
            if (st.IsEmpty)
            {
                int addable = Mathf.Min(item.MaxStack, remaining);
                remaining -= addable;
                if (remaining <= 0) return true;
            }
        }
        return remaining <= 0;
    }

    public int Add(Item item, int amount, bool notify = true)
    {
        if (item == null || amount <= 0) return amount;
        int remaining = amount;

        foreach (var st in Slots)
        {
            if (st.Item == item && st.Amount < item.MaxStack)
            {
                int addable = Mathf.Min(item.MaxStack - st.Amount, remaining);
                st.Amount += addable;
                remaining -= addable;
                if (remaining <= 0) break;
            }
        }

        foreach (var st in Slots)
        {
            if (st.IsEmpty)
            {
                int put = Mathf.Min(item.MaxStack, remaining);
                st.Item = item;
                st.Amount = put;
                remaining -= put;
                if (remaining <= 0) break;
            }
        }
        int added = amount - remaining;
        if (added > 0 && notify) NotifyChanged();
        return remaining;
    }

    public bool TryAdd(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        int remaining = Add(item, amount);
        return remaining <= 0;
    }

    public int TryRemove(Item item, int amount)
    {
        if (item == null || amount <= 0) return 0;
        int before = GetTotalAmount(item);
        RemoveUpTo(item, amount);
        int after = GetTotalAmount(item);
        return Mathf.Max(0, before - after);
    }

    public int RemoveUpTo(Item item, int amount)
    {
        if (item == null || amount <= 0) return 0;
        int remaining = amount;
        for (int i = 0; i < Slots.Count && remaining > 0; i++)
        {
            var st = Slots[i];
            if (st.Item != item) continue;
            int take = Mathf.Min(st.Amount, remaining);
            st.Amount -= take;
            remaining -= take;
            if (st.Amount <= 0) st.Clear();
        }
        int removed = amount - remaining;
        if (removed > 0) NotifyChanged();
        return removed;
    }

    public bool Remove(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        int remaining = amount;

        int total = 0;
        foreach (var st in Slots) if (st.Item == item) total += st.Amount;
        if (total < amount) return false;

        for (int i = 0; i < Slots.Count && remaining > 0; i++)
        {
            var st = Slots[i];
            if (st.Item != item) continue;
            int take = Mathf.Min(st.Amount, remaining);
            st.Amount -= take;
            remaining -= take;
            if (st.Amount <= 0) st.Clear();
        }
        NotifyChanged();
        return true;
    }

    public void ClearAt(int index)
    {
        if (index < 0 || index >= Slots.Count) return;
        if (Slots[index].IsEmpty) return;
        Slots[index].Clear();
        NotifyChanged();
    }

    public static void Swap(ItemStack a, ItemStack b)
    {
        var ai = a.Item; var aa = a.Amount;
        a.Item = b.Item; a.Amount = b.Amount;
        b.Item = ai; b.Amount = aa;
    }

    public static int Merge(ItemStack target, ItemStack source)
    {
        if (source.IsEmpty) return 0;
        if (target.IsEmpty)
        {
            int move = Mathf.Min(source.Amount, source.Item.MaxStack);
            target.Item = source.Item;
            target.Amount = move;
            source.Amount -= move;
            if (source.Amount <= 0) source.Clear();
            return source.Amount;
        }
        if (target.Item != source.Item) return source.Amount;

        int addable = Mathf.Min(target.Item.MaxStack - target.Amount, source.Amount);
        target.Amount += addable;
        source.Amount -= addable;
        if (source.Amount <= 0) source.Clear();
        return source.Amount;
    }

    public static int MoveQuantity(ItemStack from, ItemStack to, int quantity)
    {
        if (from == null || to == null) return 0;
        if (from.IsEmpty || quantity <= 0) return 0;

        if (to.IsEmpty)
        {
            int move = Mathf.Min(quantity, from.Amount, from.Item.MaxStack);
            to.Item = from.Item;
            to.Amount = move;
            from.Amount -= move;
            if (from.Amount <= 0) from.Clear();
            return move;
        }

        if (to.Item == from.Item)
        {
            int space = Mathf.Max(0, to.Item.MaxStack - to.Amount);
            int move = Mathf.Min(space, quantity, from.Amount);
            to.Amount += move;
            from.Amount -= move;
            if (from.Amount <= 0) from.Clear();
            return move;
        }

        return 0;
    }

    public int GetEmptySlotCount()
    {
        int c = 0;
        foreach (var s in Slots) if (s.IsEmpty) c++;
        return c;
    }

    public int GetExistingStackSpace(Item item)
    {
        if (item == null) return 0;
        int space = 0;
        foreach (var s in Slots)
            if (!s.IsEmpty && s.Item == item)
                space += Mathf.Max(0, item.MaxStack - s.Amount);
        return space;
    }

    public int PredictEmptySlotsAfterConsumption(Dictionary<Item, int> needsPerItem)
    {
        if (needsPerItem == null || needsPerItem.Count == 0) return 0;

        var remaining = new Dictionary<Item, int>();
        foreach (var kv in needsPerItem) if (kv.Key != null && kv.Value > 0) remaining[kv.Key] = kv.Value;

        int newEmptySlots = 0;

        for (int i = 0; i < Slots.Count; i++)
        {
            var s = Slots[i];
            if (s.IsEmpty) continue;
            if (!remaining.TryGetValue(s.Item, out int need) || need <= 0) continue;

            int take = Mathf.Min(need, s.Amount);
            int after = s.Amount - take;
            remaining[s.Item] = need - take;

            if (after <= 0) newEmptySlots++;
        }

        return newEmptySlots;
    }

    public void OnBeforeSerialize()
    {
        lastKnownCapacity = Capacity;
    }

    public void OnAfterDeserialize()
    {
        EnsureSlots();
    }
}
