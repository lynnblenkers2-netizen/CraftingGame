using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceEntryUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI nameText;
    [HideInInspector] public ResourceDefinition def;

    public void Bind(ResourceDefinition d, string initial, Sprite overrideIcon = null, string overrideName = null)
    {
        def = d;
        if (icon) icon.sprite = overrideIcon ? overrideIcon : (d ? d.icon : null);
        if (valueText) valueText.text = initial ?? "0";
        if (nameText)
        {
            string display = !string.IsNullOrEmpty(overrideName)
                ? overrideName
                : (d ? (!string.IsNullOrEmpty(d.displayName) ? d.displayName : d.id) : string.Empty);
            nameText.text = display;
        }
    }

    public void SetValue(int v, string format = "n0")
    {
        if (valueText) valueText.text = v.ToString(format);
    }
}
