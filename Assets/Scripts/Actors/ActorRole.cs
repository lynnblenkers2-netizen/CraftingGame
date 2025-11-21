using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Role", fileName = "Role_")]
public class ActorRole : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite portrait;

    [Header("Capabilities")]
    public bool canForage = true;
    public bool canSell = true;
    public bool canResearch = true;
    public bool canCraft = true;
    public bool canVisitTavern = true;

    [Header("Base Stats")]
    [Tooltip("Affects travel/work duration. 1.0 = baseline; higher is faster.")]
    [Min(0.1f)] public float efficiency = 1.0f;

    [Tooltip("Backpack capacity (stacks).")]
    [Min(1)] public int backpackSlots = 6;
}
