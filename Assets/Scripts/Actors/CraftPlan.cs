using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Tasks/Craft Plan", fileName = "Craft_")]
public class CraftPlan : ScriptableObject
{
    public string id;
    public string displayName;
    public ShapedRecipe recipe;

    [Header("Work cadence")]
    [Min(0.05f)] public float daysPerCraft = 0.2f;

    [Header("Spirit fuel")]
    [Min(0)] public int spiritPerCraft = 0;

    [Header("Backpack IO")]
    public bool takeInputsFromBackpackFirst = true;
    public bool putOutputsIntoBackpack = true;
}
