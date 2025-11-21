using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerProgress : MonoBehaviour
{
    [Header("XP Curve")]
    public int startLevel = 1;
    public int startXP = 0;
    public AnimationCurve xpCurve = AnimationCurve.EaseInOut(1, 10, 50, 1000);
    public int skillpointsPerLevel = 1;

    [Header("Refs")]
    public TreeState skillTree;
    [Header("Skill Config")]
    [SerializeField] private List<SkillDefinition> craftingSkills = new();
    [SerializeField] private List<SkillDefinition> forageSkills = new();

    public int Level { get; private set; }
    public int XP { get; private set; }
    public event Action OnChanged;

    void Awake()
    {
        Level = startLevel;
        XP = startXP;
        OnChanged?.Invoke();
    }

    public void GrantXP(int amount)
    {
        XP += Mathf.Max(0, amount);
        while (XP >= RequiredXPForLevel(Level + 1))
        {
            Level++;
            if (skillTree)
            {
                skillTree.points += skillpointsPerLevel;
                skillTree.RaiseChanged();
            }
            ToastSystem.Success("Level up!", $"You reached Level {Level}. +{skillpointsPerLevel} SP");
        }
        OnChanged?.Invoke();
    }

    public int RequiredXPForLevel(int targetLevel)
    {
        float x = Mathf.Clamp(targetLevel, 1, 1000);
        return Mathf.RoundToInt(xpCurve.Evaluate(x));
    }

    // Returns additive chance and multiplier bonuses from unlocked crafting skills
    public void GetCraftingBonuses(out float chance, out float multiplier)
    {
        chance = 0f;
        multiplier = 1f;
        if (skillTree == null || craftingSkills == null) return;

        foreach (var s in craftingSkills)
        {
            if (s == null || s.effect != SkillEffectType.DoubleCraftOutput) continue;
            if (!skillTree.IsUnlocked(s.id)) continue;
            chance += Mathf.Max(0f, s.effectChance);
            multiplier += Mathf.Max(0f, s.effectMultiplier - 1f);
        }

        chance = Mathf.Clamp01(chance);
    }

    // Returns additive chance and multiplier bonuses from unlocked forage skills
    public void GetForageBonuses(out float chance, out float multiplier)
    {
        chance = 0f;
        multiplier = 1f;
        if (skillTree == null || forageSkills == null) return;

        foreach (var s in forageSkills)
        {
            if (s == null || s.effect != SkillEffectType.DoubleForageYield) continue;
            if (!skillTree.IsUnlocked(s.id)) continue;
            chance += Mathf.Max(0f, s.effectChance);
            multiplier += Mathf.Max(0f, s.effectMultiplier - 1f);
        }

        chance = Mathf.Clamp01(chance);
    }
}
