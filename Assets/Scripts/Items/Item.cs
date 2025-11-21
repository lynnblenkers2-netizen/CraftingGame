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
    
    [Header("Description")]
    [TextArea(2,6)]
    [Tooltip("Long description shown in tooltips.")]
    public string description;
}