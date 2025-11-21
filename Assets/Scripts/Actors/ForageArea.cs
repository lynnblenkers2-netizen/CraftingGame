using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/Actors/Tasks/Forage Area", fileName = "Area_")]
public class ForageArea : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Base Duration (game days)")]
    [Min(0.1f)] public float travelDays = 0.5f;
    [Min(0.1f)] public float workDays = 1.0f;
    public bool roundTrip = true;

    [Header("Season Multipliers (index = calendar season)")]
    public float[] seasonYieldMul = new float[] { 1, 1, 1, 1 };

    [Serializable]
    public struct Drop
    {
        public Item item;
        public int min;
        public int max;
        [Tooltip("Chance [0-1] that this drop occurs on each forage tick. 0 or negative = always drops.")]
        public float dropChance;
    }

    [Header("Loot Table")]
    public Drop[] drops;

    public int RollAmount(System.Random rng, float seasonMul, Drop d)
    {
        var baseAmt = rng.Next(d.min, d.max + 1);
        return Mathf.Max(0, Mathf.RoundToInt(baseAmt * seasonMul));
    }
}
