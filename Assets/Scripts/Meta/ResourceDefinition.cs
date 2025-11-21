using UnityEngine;

[CreateAssetMenu(menuName ="Game/Meta/Resource", fileName ="RES_") ]
public class ResourceDefinition: ScriptableObject
{
    public string id ="gold";
    public Sprite icon;
    public string displayName ="Gold";

    [Header("Stacking")]
    [Min(1)] public int maxStack = 99;

    [Header("Description")]
    [TextArea(2,6)]
    [Tooltip("Description shown in tooltips for this resource.")]
    public string description;
}
