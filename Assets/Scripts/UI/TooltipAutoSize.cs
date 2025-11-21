using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TooltipAutoSize : MonoBehaviour
{
    [Header("Child References (drag & drop in prefab)")]
    public Image icon;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;

    [Header("Sizing")]
    public RectTransform backgroundRect; // optional: the RectTransform to resize (defaults to this)
    public float paddingVertical = 12f;
    public float paddingHorizontal = 12f;
    public float maxWidth = 260f;

    void Reset()
    {
        if (backgroundRect == null) backgroundRect = GetComponent<RectTransform>();
    }

    // Set both title and description; description is used to compute preferred height (title included)
    public void SetText(string titleStr, string descStr)
    {
        if (title != null) title.text = titleStr ?? "";
        if (description != null) description.text = descStr ?? "";
        // Ensure background rect is available
        var root = backgroundRect != null ? backgroundRect : GetComponent<RectTransform>();
        if (!root) return;

        // Calculate content max width (inside padding)
        float contentMaxWidth = Mathf.Max(1f, maxWidth - paddingHorizontal * 2f);

        // Set child widths so TMP will wrap to the intended width
        if (title != null) title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentMaxWidth);
        if (description != null) description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentMaxWidth);

        // Force TMP to update its internal layout
        if (title != null) title.ForceMeshUpdate();
        if (description != null) description.ForceMeshUpdate();

        // Measure title & description using TMP preferred values with wrapping
        float titleH = (title != null && !string.IsNullOrEmpty(titleStr))
            ? title.GetPreferredValues(titleStr, contentMaxWidth, 10000f).y
            : 0f;

        float descH = (description != null && !string.IsNullOrEmpty(descStr))
            ? description.GetPreferredValues(descStr, contentMaxWidth, 10000f).y
            : 0f;

        // Place elements manually (no layout group on prefab)
        float spacing = (titleH > 0f && descH > 0f) ? 6f : 0f;
        float y = -paddingVertical;

        if (title != null && titleH > 0f)
        {
            var tr = title.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0f, 1f);
            tr.pivot = new Vector2(0f, 1f);
            float rowH = Mathf.Max(titleH, 24f);
            tr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentMaxWidth);
            tr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowH);
            tr.anchoredPosition = new Vector2(paddingHorizontal, y);
            y -= rowH + spacing;
        }

        if (description != null && descH > 0f)
        {
            var dr = description.rectTransform;
            dr.anchorMin = dr.anchorMax = new Vector2(0f, 1f);
            dr.pivot = new Vector2(0f, 1f);
            float dH = descH;
            dr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentMaxWidth);
            dr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dH);
            dr.anchoredPosition = new Vector2(paddingHorizontal, y);
            y -= dH;
        }

        // Compose final sizes (no layout group on prefab; we add our own spacing)
        float contentHeight = (titleH > 0f ? titleH : 0f) + (descH > 0f ? descH : 0f) + spacing;
        float finalHeight = contentHeight + paddingVertical * 2f;
        float finalWidth = Mathf.Min(maxWidth, contentMaxWidth + paddingHorizontal * 2f);

        // Apply calculated size to root
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, finalHeight));

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }
}
