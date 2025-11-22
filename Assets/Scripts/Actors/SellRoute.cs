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
    [Tooltip("Price multipliers per item tier on this route. If no entry exists for a tier, defaultMultiplier is used.")]
    public PriceEntry[] prices;
    [Tooltip("Multiplier used when no tier-specific entry is found.")]
    public float defaultMultiplier = 1f;
    [Tooltip("Fallback base price if the item has no own price set.")]
    public int defaultPrice = 1;

    [System.Serializable]
    public struct PriceEntry
    {
        [Min(1)] public int tier;
        public float multiplier;
    }

    public int GetPrice(Item it)
    {
        if (it == null) return defaultPrice;

        float mult = defaultMultiplier;
        int tier = Mathf.Max(1, it.tier);
        if (prices != null)
        {
            for (int i = 0; i < prices.Length; i++)
            {
                var p = prices[i];
                if (p.tier == tier) { mult = p.multiplier; break; }
            }
        }

        int basePrice = it.price > 0 ? it.price : defaultPrice;
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * mult));
    }
}
