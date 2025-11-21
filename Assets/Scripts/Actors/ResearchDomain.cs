using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Tasks/Research Domain", fileName = "Research_")]
public class ResearchDomain : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Duration (game days)")]
    [Min(0.1f)] public float days = 1.5f;

    [Header("Rewards")]
    public ShapedRecipe[] possibleRecipes;
    [Range(0, 1)] public float recipeChance = 0.6f;

    [Tooltip("Optional: Spirit generator producer to unlock.")]
    public ProducerDefinition[] possibleSpiritGenerators;
    [Range(0, 1)] public float spiritGenChance = 0.2f;
}
