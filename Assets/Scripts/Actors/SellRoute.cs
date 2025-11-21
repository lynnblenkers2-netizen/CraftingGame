using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Tasks/Sell Route", fileName = "Route_")]
public class SellRoute : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Trip (game days)")]
    [Min(0.1f)] public float travelDays = 0.5f;
    [Min(0.1f)] public float marketDays = 0.3f;
    public bool roundTrip = true;

    [Header("Pricing")]
    [Tooltip("Gold per item id; if not found → defaultPrice.")]
    public PriceEntry[] prices;
    public int defaultPrice = 1;

    [System.Serializable]
    public struct PriceEntry
    {
        public Item item;
        public int goldPerItem;
    }

    public int GetPrice(Item it)
    {
        if (prices != null)
        {
            foreach (var p in prices)
            {
                if (p.item == it) return p.goldPerItem;
            }
        }
        return defaultPrice;
    }
}
