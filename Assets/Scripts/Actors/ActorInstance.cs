using UnityEngine;
using System;
using System.Collections.Generic;

public enum ActorState { Idle, Traveling, Working, Returning, Paused }

[Serializable]
public class ActorInstance
{
    public string instanceId;
    public ActorDefinition def;
    public ActorRole Role => def ? def.role : null;

    [Header("Progression")]
    public int level = 1;
    public int xp = 0;

    [Header("State")]
    public ActorState state = ActorState.Idle;
    public float remainingDays;
    public string taskType;
    public ScriptableObject taskAsset;
    public bool repeatTask;

    [Header("Backpack")]
    public Inventory backpack;
    [Header("Equipment")]
    public Inventory equipment;
    [Header("Equipment Totals (debug)")]
    [SerializeField] EquipmentTotals lastEquipmentTotals;

    [Header("Statements")]
    public List<string> statementLog = new();
    public event Action<string> OnStatement;
    public string lastHaulSummary = "nothing";
    public const string ThoughtTag = "[THOUGHT]";
    string lastThoughtDisplay;
    float thoughtExpiresAt;
    public float nextIdleThoughtTime;

    [Header("Mood")]
    public float happinessProgress;
    public float happinessBonus;
    public float EffectiveHappiness => Mathf.Clamp01((def ? def.happiness : 0f) + happinessBonus);

    public ActorInstance(ActorDefinition d)
    {
        instanceId = Guid.NewGuid().ToString("N");
        def = d;
        EnsureBackpackCapacity();
        EnsureEquipmentSlots();
        AddStatement($"{def?.displayName ?? "Actor"} ready for duty.");
    }

    int GetDesiredBackpackSlots()
    {
        var role = def && def.role ? def.role : null;
        return Mathf.Max(1, role ? role.backpackSlots : 1);
    }

    public void EnsureBackpackCapacity()
    {
        int slots = GetDesiredBackpackSlots();
        if (backpack == null) backpack = new Inventory(slots);
        else if (backpack.Capacity != slots) backpack.SetCapacity(slots);
    }

    public void EnsureEquipmentSlots()
    {
        const int EquipmentSlots = 3;
        if (equipment == null) equipment = new Inventory(EquipmentSlots);
        else if (equipment.Capacity != EquipmentSlots) equipment.SetCapacity(EquipmentSlots);
    }

    public void AddStatement(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (statementLog == null) statementLog = new List<string>();
        statementLog.Add(message);
        if (statementLog.Count > 50) statementLog.RemoveAt(0);
        OnStatement?.Invoke(message);
    }

    public void AddThought(string stampedMessage, float displaySeconds = 8f)
    {
        if (string.IsNullOrWhiteSpace(stampedMessage)) return;
        lastThoughtDisplay = StripTimestampAndTag(stampedMessage);
        thoughtExpiresAt = Time.realtimeSinceStartup + Mathf.Max(1f, displaySeconds);
        AddStatement($"{ThoughtTag}{stampedMessage}");
    }

    public bool ThoughtActive => !string.IsNullOrEmpty(lastThoughtDisplay) && Time.realtimeSinceStartup < thoughtExpiresAt;
    public string CurrentThought => ThoughtActive ? lastThoughtDisplay : null;

    public static bool IsThoughtMessage(string message) => !string.IsNullOrEmpty(message) && message.StartsWith(ThoughtTag, StringComparison.Ordinal);
    public static string StripThoughtTag(string message) => IsThoughtMessage(message) ? message.Substring(ThoughtTag.Length) : message;

    public void AddHappinessProgress(float amount)
    {
        if (amount <= 0f) return;
        happinessProgress += amount;
        happinessBonus = Mathf.Clamp01(Mathf.Log10(1f + happinessProgress) * 0.25f);
    }

    static string StripTimestampAndTag(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        string msg = StripThoughtTag(message);
        int end = msg.IndexOf(']');
        if (end >= 0 && end + 1 < msg.Length)
        {
            int start = end + 1;
            if (msg[start] == ' ') start++;
            return msg.Substring(start);
        }
        return msg;
    }

    public bool GainXP(int amount, out bool leveledUp)
    {
        leveledUp = false;
        if (amount <= 0) return false;
        xp += amount;
        while (xp >= RequiredXPForNextLevel())
        {
            xp -= RequiredXPForNextLevel();
            level++;
            leveledUp = true;
        }
        return true;
    }

    int RequiredXPForNextLevel()
    {
        // Simple progression: base 10 + 5 per current level
        return 10 + Mathf.Max(0, level - 1) * 5;
    }

    public void Dispose()
    {
        backpack = null;
        equipment = null;
        statementLog?.Clear();
        OnStatement = null;
    }

    public EquipmentTotals GetEquipmentTotals()
    {
        var totals = new EquipmentTotals();
        if (equipment == null) return totals;
        foreach (var st in equipment.Slots)
        {
            if (st == null || st.IsEmpty || st.Item == null) continue;
            var b = st.Item.equipmentBonuses;
            int count = Mathf.Max(1, st.Amount);
            totals.travelSpeed += b.travelSpeed * count;
            totals.craftingSpeed += b.craftingSpeed * count;
            totals.researchSpeed += b.researchSpeed * count;
            totals.forageSpeed += b.forageSpeed * count;
            totals.forageLuck += b.forageLuck * count;
            totals.tavernLuck += b.tavernLuck * count;
            totals.researchLuck += b.researchLuck * count;
        }
        lastEquipmentTotals = totals;
        return totals;
    }

    [System.Serializable]
    public struct EquipmentTotals
    {
        public float travelSpeed;
        public float craftingSpeed;
        public float researchSpeed;
        public float forageSpeed;
        public float forageLuck;
        public float tavernLuck;
        public float researchLuck;
    }
}
