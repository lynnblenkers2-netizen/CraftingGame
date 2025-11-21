using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName="Game/Skills/Skill", fileName="Skill_")]
public class SkillDefinition : ScriptableObject
{
    public string id;              // unique (z.B. "skill_dash")
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;
    public int cost = 1;           // Skillpunkte
    public List<SkillDefinition> prerequisites = new(); // direkte Vorbedingungen
    [Header("Editor Layout")]
    public Vector2 canvasPosition; // Position im Content (Tree-Canvas)

    [Header("Effect (optional)")]
    public SkillEffectType effect = SkillEffectType.None;
    [Tooltip("Chance (0-1) the effect triggers when relevant action happens.")]
    [Range(0f, 1f)] public float effectChance = 0.2f;
    [Tooltip("Multiplier applied when effect triggers (e.g., 2 = double).")]
    public float effectMultiplier = 2f;
}

public enum SkillEffectType
{
    None = 0,
    DoubleCraftOutput = 1,
    DoubleForageYield = 2,
}
