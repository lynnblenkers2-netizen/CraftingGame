using UnityEngine;

[CreateAssetMenu(menuName = "Game/Meta/Producer", fileName = "Producer_")]
public class ProducerDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    [Header("Runtime")]
    [Tooltip("Spirit generated per second by this producer (can be fractional).")]
    public float spiritPerSecond = 0f;

    [TextArea(3,6)]
    [Tooltip("Description shown in the tooltip when this producer item is inspected.")]
    public string description;
}
