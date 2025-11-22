using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item")]
public class Item : ScriptableObject
{
    [Header("ID/Anzeige")]
    public string Id;
    public string DisplayName;

    [Header("Darstellung")]
    public Sprite Icon;

    [Header("Stacking")]
    [Min(1)] public int MaxStack = 99;

    [Header("Producer (optional)")]
    [Tooltip("Optional: attach a ProducerDefinition to make this Item act as a spirit generator when placed in a socket.")]
    public ProducerDefinition producer;
    
    [Header("Progression")]
    [Min(0)]
    [Tooltip("Progression tier of this item (used for gating/unlocks).")]
    public int tier = 0;

    [Header("Equipment")]
    [Tooltip("If true, this item can be equipped into an actor's equipment slot.")]
    public bool equippable = false;

    [System.Serializable]
    public struct EquipmentBonuses
    {
        [Tooltip("Multiplier bonus to travel speed (0.1 = +10%).")] public float travelSpeed;
        [Tooltip("Multiplier bonus to crafting speed (0.1 = +10%).")] public float craftingSpeed;
        [Tooltip("Multiplier bonus to research speed (0.1 = +10%).")] public float researchSpeed;
        [Tooltip("Multiplier bonus to forage speed (0.1 = +10%).")] public float forageSpeed;
        [Tooltip("Additive bonus to forage luck (0.1 = +10% drop chance).")] public float forageLuck;
        [Tooltip("Additive bonus to tavern luck (0.1 = +10% lead/offer chance).")] public float tavernLuck;
        [Tooltip("Additive bonus to research luck (0.1 = +10% discovery chance).")] public float researchLuck;
    }

    [Header("Equipment Bonuses")]
    public EquipmentBonuses equipmentBonuses;

    [Header("Equipment Bonus Descriptions")]
    [Tooltip("Text shown for travel speed bonus in tooltips.")]
    public string travelSpeedDescription = "Travel speed";
    [Tooltip("Text shown for crafting speed bonus in tooltips.")]
    public string craftingSpeedDescription = "Crafting speed";
    [Tooltip("Text shown for research speed bonus in tooltips.")]
    public string researchSpeedDescription = "Research speed";
    [Tooltip("Text shown for forage speed bonus in tooltips.")]
    public string forageSpeedDescription = "Forage speed";
    [Tooltip("Text shown for forage luck bonus in tooltips.")]
    public string forageLuckDescription = "Forage luck";
    [Tooltip("Text shown for tavern luck bonus in tooltips.")]
    public string tavernLuckDescription = "Tavern luck";
    [Tooltip("Text shown for research luck bonus in tooltips.")]
    public string researchLuckDescription = "Research luck";

    [Header("Designer Notes")]
    [Tooltip("Bonus values are fractional: 0.15 = +15%, 1.0 = +100%. They are added to 1.0 internally (speed = 1 + bonus; luck multipliers = 1 + bonus).")]
    [TextArea(2,4)] public string equipmentBonusNote = "Enter fractional bonuses (e.g., 0.15 = +15%, 1.0 = +100%). Speed bonuses multiply duration denominators; luck bonuses multiply chances.";

    [Header("Pricing")]
    [Min(0)]
    [Tooltip("Gold value of this item when sold (used if no route-specific price is set).")]
    public int price = 0;

    [Header("Description")]
    [TextArea(2,6)]
    [Tooltip("Long description shown in tooltips.")]
    public string description;
}
