using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Actor", fileName = "Actor_")]
public class ActorDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public ActorRole role;
    public Sprite portraitOverride;

    [Header("Maintenance")]
    [Tooltip("Spirit consumed by this actor per game day.")]
    public int spiritPerDay = 0;
    [Tooltip("Gold required to hire this actor.")]
    public int hireGoldCost = 0;

    [Header("Personality")]
    [Range(0f, 1f)] public float happiness = 0.5f;
    [Range(0f, 1f)] public float clumsiness = 0.2f;
    [Tooltip("Random thoughts the actor may express when happy.")]
    public string[] happinessThoughts;
    [Tooltip("Random thoughts the actor may express when clumsy.")]
    public string[] clumsyThoughts;
    [Header("Thought Timing (Idle)")]
    [Min(1f)] public float idleThoughtIntervalMinSeconds = 17f;
    [Min(1f)] public float idleThoughtIntervalMaxSeconds = 35f;
    [Header("Idle Thoughts")]
    public string[] happyIdleThoughts;
    public string[] clumsyIdleThoughts;
    [Header("Task-Specific Happy Thoughts")]
    public string[] happyTravelThoughts;
    public string[] happyForageThoughts;
    public string[] happySellThoughts;
    public string[] happyResearchThoughts;
    public string[] happyCraftThoughts;
    public string[] happyTavernThoughts;
    [Header("Task-Specific Clumsy Thoughts")]
    public string[] clumsyTravelThoughts;
    public string[] clumsyForageThoughts;
    public string[] clumsySellThoughts;
    public string[] clumsyResearchThoughts;
    public string[] clumsyCraftThoughts;
    public string[] clumsyTavernThoughts;
}
