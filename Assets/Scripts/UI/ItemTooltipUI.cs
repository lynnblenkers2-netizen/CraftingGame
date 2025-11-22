using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Text;

// Lightweight tooltip manager for item hover details
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }
    [Tooltip("Prefab for the tooltip. Assign a prefab that contains a TooltipAutoSize component.")]
    [SerializeField] GameObject tooltipPrefab;

    [Tooltip("Optional root canvas to parent the tooltip to. If empty the first Canvas in scene will be used.")]
    [SerializeField] Canvas rootCanvas;

    GameObject tooltipInstance;
    RectTransform canvasRect;
    RectTransform tooltipRect;
    Image iconImage;
    TextMeshProUGUI titleText;
    TextMeshProUGUI descriptionText;
    TooltipAutoSize autoSizer;

    void Awake()
    {
        // Only initialize singleton during play mode to avoid editor-time side effects
        if (!Application.isPlaying) return;

        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        if (rootCanvas != null) canvasRect = rootCanvas.GetComponent<RectTransform>();
    }

    void OnDestroy()
    {
        try {
            if (Instance == this) Instance = null;
        } catch { }
    }

    void OnDisable()
    {
        // Hide tooltip if component is disabled (editor or play)
        try
        {
            if (tooltipInstance != null)
                tooltipInstance.SetActive(false);
        }
        catch { }
    }

    // Show tooltip for given stack at screen position (pointer position)
    public void Show(object stackObj, Vector2 screenPosition)
    {
        try
        {
        // maintain backward compatibility by dispatching based on concrete types
        if (stackObj is ItemStack its)
        {
            Show(its, screenPosition);
            return;
        }
        if (stackObj is Item it)
        {
            Show(it, screenPosition);
            return;
        }
        if (stackObj is ResourceEntryUI re)
        {
            Show(re, screenPosition);
            return;
        }
        if (stackObj is ResourceDefinition rd)
        {
            Show(rd, screenPosition);
            return;
        }

        // fallback: try to create tooltip and display ToString
        EnsureTooltipCreated();
        if (titleText != null) titleText.text = stackObj != null ? stackObj.ToString() : string.Empty;
        if (autoSizer != null)
            autoSizer.SetText(titleText != null ? titleText.text : string.Empty, descriptionText != null ? descriptionText.text : string.Empty);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }

    void EnsureTooltipCreated()
    {
        if (tooltipPrefab == null) return;
        if (tooltipInstance != null) return;

        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
            {
                // In edit mode, don't spawn a hidden canvas; just bail to avoid inspector errors
                if (!Application.isPlaying) return;
                rootCanvas = CreateFallbackCanvas();
                if (rootCanvas == null) return;
            }
        }
        tooltipInstance = Instantiate(tooltipPrefab, rootCanvas.transform);
        canvasRect = rootCanvas.GetComponent<RectTransform>();
        tooltipRect = tooltipInstance.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            // simplify positioning math: keep tooltip anchored to canvas center
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        }

        autoSizer = tooltipInstance.GetComponent<TooltipAutoSize>();
        if (autoSizer != null)
        {
            iconImage = autoSizer.icon;
            titleText = autoSizer.title;
            descriptionText = autoSizer.description;
        }
        else
        {
            var t = tooltipInstance.transform;
            var iconTf = t.Find("Icon"); if (iconTf) iconImage = iconTf.GetComponent<Image>();
            var titleTf = t.Find("Title"); if (titleTf) titleText = titleTf.GetComponent<TextMeshProUGUI>();
            var descTf = t.Find("Description"); if (descTf) descriptionText = descTf.GetComponent<TextMeshProUGUI>();
        }
    }

    Canvas CreateFallbackCanvas()
    {
        // Create a hidden overlay canvas if none exists (avoids needing a scene reference on the prefab)
        var go = new GameObject("TooltipCanvas (auto)");
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    // Show tooltip for an ItemStack
    public void Show(ItemStack stack, Vector2 screenPosition)
    {
        if (stack == null || stack.IsEmpty) return;
        EnsureTooltipCreated();
        PopulateFromItem(stack.Item, stack.Amount);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
    }

    // Generic string/icon overload for non-item tooltips (e.g., skills)
    public void Show(string title, string desc, Sprite iconSprite, Vector2 screenPosition = default)
    {
        EnsureTooltipCreated();
        if (iconImage) iconImage.sprite = iconSprite;
        if (titleText) titleText.text = title ?? string.Empty;
        if (descriptionText) descriptionText.text = desc ?? string.Empty;
        autoSizer?.SetText(titleText != null ? titleText.text : string.Empty, descriptionText != null ? descriptionText.text : string.Empty);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
    }

    // Show tooltip for an Item directly
    public void Show(Item item, Vector2 screenPosition, int amount = 0)
    {
        if (item == null) return;
        EnsureTooltipCreated();
        PopulateFromItem(item, amount);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
    }

    // Show tooltip for ResourceEntryUI row
    public void Show(ResourceEntryUI entry, Vector2 screenPosition)
    {
        if (entry == null) return;
        EnsureTooltipCreated();
        PopulateFromResourceEntry(entry);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
    }

    // Show tooltip for ResourceDefinition
    public void Show(ResourceDefinition def, Vector2 screenPosition, string valueText = null)
    {
        if (def == null) return;
        EnsureTooltipCreated();
        PopulateFromResourceDefinition(def, valueText);
        if (tooltipInstance != null) tooltipInstance.SetActive(true);
        UpdatePosition(screenPosition);
    }

    void PopulateFromItem(Item item, int amount)
    {
        if (iconImage) iconImage.sprite = item.Icon;
        if (titleText) titleText.text = !string.IsNullOrEmpty(item.DisplayName) ? item.DisplayName : item.Id;
        if (descriptionText)
        {
            string desc = null;
            if (item.producer != null && !string.IsNullOrEmpty(item.producer.description))
                desc = item.producer.description;
            else if (!string.IsNullOrEmpty(item.description))
                desc = item.description;
            else if (amount > 0)
                desc = amount.ToString();

            string bonusText = BuildEquipmentBonusText(item);
            string priceLine = $"Price: {item.price} gold";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(desc)) sb.Append(desc);
            if (!string.IsNullOrEmpty(bonusText))
            {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(bonusText);
            }
            if (sb.Length > 0) sb.Append("\n");
            sb.Append(priceLine);

            descriptionText.text = sb.ToString();
        }
        if (autoSizer != null)
            autoSizer.SetText(titleText != null ? titleText.text : string.Empty, descriptionText != null ? descriptionText.text : string.Empty);
    }

    string BuildEquipmentBonusText(Item item)
    {
        if (item == null || !item.equippable) return null;
        var b = item.equipmentBonuses;
        var sb = new StringBuilder();

        void Append(string label, float value)
        {
            if (Mathf.Approximately(value, 0f)) return;
            if (sb.Length > 0) sb.Append("\n");
            sb.Append(label).Append(": ").Append(FormatPercent(value));
        }

        Append(string.IsNullOrWhiteSpace(item.travelSpeedDescription) ? "Travel speed" : item.travelSpeedDescription, b.travelSpeed);
        Append(string.IsNullOrWhiteSpace(item.craftingSpeedDescription) ? "Crafting speed" : item.craftingSpeedDescription, b.craftingSpeed);
        Append(string.IsNullOrWhiteSpace(item.researchSpeedDescription) ? "Research speed" : item.researchSpeedDescription, b.researchSpeed);
        Append(string.IsNullOrWhiteSpace(item.forageSpeedDescription) ? "Forage speed" : item.forageSpeedDescription, b.forageSpeed);
        Append(string.IsNullOrWhiteSpace(item.forageLuckDescription) ? "Forage luck" : item.forageLuckDescription, b.forageLuck);
        Append(string.IsNullOrWhiteSpace(item.tavernLuckDescription) ? "Tavern luck" : item.tavernLuckDescription, b.tavernLuck);
        Append(string.IsNullOrWhiteSpace(item.researchLuckDescription) ? "Research luck" : item.researchLuckDescription, b.researchLuck);

        return sb.Length > 0 ? sb.ToString() : null;
    }

    string FormatPercent(float value)
    {
        float pct = value * 100f;
        return pct >= 0f ? $"+{pct:0.#}%" : $"{pct:0.#}%";
    }

    void PopulateFromResourceEntry(ResourceEntryUI entry)
    {
        if (entry == null) return;
        if (iconImage) iconImage.sprite = entry.icon ? entry.icon.sprite : null;
        if (titleText) titleText.text = entry.def ? (!string.IsNullOrEmpty(entry.def.displayName) ? entry.def.displayName : entry.def.id) : string.Empty;
        if (descriptionText)
        {
            if (entry.def != null && !string.IsNullOrEmpty(entry.def.description))
                descriptionText.text = entry.def.description;
            else
                descriptionText.text = entry.valueText ? entry.valueText.text : string.Empty;
        }
        if (autoSizer != null)
            autoSizer.SetText(titleText != null ? titleText.text : string.Empty, descriptionText != null ? descriptionText.text : string.Empty);
    }

    void PopulateFromResourceDefinition(ResourceDefinition def, string valueText)
    {
        if (def == null) return;
        if (iconImage) iconImage.sprite = def.icon;
        if (titleText) titleText.text = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.id;
        if (descriptionText) descriptionText.text = !string.IsNullOrEmpty(def.description) ? def.description : (valueText ?? string.Empty);
        if (autoSizer != null)
            autoSizer.SetText(titleText != null ? titleText.text : string.Empty, descriptionText != null ? descriptionText.text : string.Empty);
    }

    // Update shown tooltip's screen position
    public void UpdatePosition(Vector2 screenPosition)
    {
        try
        {
            if (tooltipInstance == null || tooltipRect == null || canvasRect == null) return;

            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out var local))
            {
                // small offset so pointer doesn't overlap
                var anchored = local + new Vector2(12f, -12f);

                // clamp to canvas bounds using pivot
                var size = tooltipRect.rect.size;
                var pivot = tooltipRect.pivot;
                var halfCanvas = canvasRect.rect.size * 0.5f;

                float minX = anchored.x - pivot.x * size.x;
                float maxX = anchored.x + (1f - pivot.x) * size.x;
                float minY = anchored.y - pivot.y * size.y;
                float maxY = anchored.y + (1f - pivot.y) * size.y;

                if (maxX > halfCanvas.x) anchored.x -= (maxX - halfCanvas.x);
                if (minX < -halfCanvas.x) anchored.x += (-halfCanvas.x - minX);
                if (maxY > halfCanvas.y) anchored.y -= (maxY - halfCanvas.y);
                if (minY < -halfCanvas.y) anchored.y += (-halfCanvas.y - minY);

                tooltipRect.anchoredPosition = anchored;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }

    public void Hide()
    {
        try
        {
            if (!Application.isPlaying) return;
            if (tooltipInstance != null) tooltipInstance.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }
}
