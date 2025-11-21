using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Skills/Tree State", fileName="SkillTreeState")]
public class TreeState : ScriptableObject
{
    [Header("Runtime State")]
    public int points = 0;

    [SerializeField] private List<string> unlockedIds = new();
    [SerializeField] private List<string> queuedIds   = new(); // vorgemerkte (noch nicht bestätigte)

    public event Action OnChanged;      // alles (Punkte/Unlocks/Queue)
    public event Action OnQueueChanged; // nur Queue/UI

    public IReadOnlyList<string> Unlocked => unlockedIds;
    public IReadOnlyList<string> Queued   => queuedIds;

    public void RaiseChanged() => OnChanged?.Invoke();

    public bool IsUnlocked(string id) => unlockedIds.Contains(id);
    public bool IsQueued(string id)   => queuedIds.Contains(id);

    public int GetQueuedTotalCost(Func<string,int> costLookup)
    {
        int sum = 0;
        foreach (var id in queuedIds) sum += Mathf.Max(0, costLookup?.Invoke(id) ?? 0);
        return sum;
    }

    public int GetAvailablePoints(Func<string,int> costLookup)
    {
        return Mathf.Max(0, points - GetQueuedTotalCost(costLookup));
    }

    // --- Mutationen ---
    public void GrantPoints(int amount)
    {
        if (amount <= 0) return;
        points += amount;
        OnChanged?.Invoke();
    }

    public void QueueUnlock(string id)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id) || IsQueued(id)) return;
        queuedIds.Add(id);
        OnQueueChanged?.Invoke();
        OnChanged?.Invoke();
    }

    public void Unqueue(string id)
    {
        if (queuedIds.Remove(id))
        {
            OnQueueChanged?.Invoke();
            OnChanged?.Invoke();
        }
    }

    public void ClearQueue()
    {
        if (queuedIds.Count == 0) return;
        queuedIds.Clear();
        OnQueueChanged?.Invoke();
        OnChanged?.Invoke();
    }

    public void ApplyQueue(Func<string,int> costLookup)
    {
        // Prüfe Punkte
        int need = GetQueuedTotalCost(costLookup);
        if (need > points) return;

        points -= need;
        foreach (var id in queuedIds)
            if (!unlockedIds.Contains(id)) unlockedIds.Add(id);

        queuedIds.Clear();
        OnChanged?.Invoke();
    }

    public int ComputeSpentPoints(Func<string,int> costLookup)
    {
        int sum = 0;
        foreach (var id in unlockedIds)
            sum += Mathf.Max(0, costLookup != null ? costLookup(id) : 0);
        return sum;
    }

    /// <summary>
    /// Setzt alle Skills zurück und schreibt ihre Kosten den verfügbaren Punkten gut.
    /// Queued wird geleert (hat ohnehin keine Punkte verbraucht).
    /// </summary>
    public void Respec(Func<string,int> costLookup)
    {
        int refund = ComputeSpentPoints(costLookup);
        points += refund;

        unlockedIds.Clear();
        queuedIds.Clear();

        OnChanged?.Invoke();
    }
}
