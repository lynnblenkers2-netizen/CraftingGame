using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

public class TreeNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Refs")]
    public Image icon;
    public Image frame;
    public Button button;
    public TextMeshProUGUI label;
    public TextMeshProUGUI costText;

    [HideInInspector] public SkillDefinition def;
    [HideInInspector] public TreePanelController controller;

    public void Bind(SkillDefinition d, TreePanelController c)
    {
        def = d; controller = c;
        if (icon) icon.sprite = d.icon;
        if (label) label.text = d.displayName;
        if (costText) costText.text = d.cost > 0 ? d.cost.ToString() : "";
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (controller) controller.ToggleQueue(def);
    }

    void ShowTooltip(Vector2 screenPos)
    {
        if (def == null) return;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(def.description)) sb.Append(def.description);
        if (def.cost > 0)
        {
            if (sb.Length > 0) sb.AppendLine().AppendLine();
            sb.Append("Cost: ").Append(def.cost).Append(" SP");
        }
        ItemTooltipUI.Instance?.Show(def.displayName ?? def.id, sb.ToString(), def.icon, screenPos);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!controller || controller.state == null || def == null) return;
        var st = controller.state;
        bool unlocked = st.IsUnlocked(def.id);
        bool queued = st.IsQueued(def.id);
        if (unlocked || queued) return;

        bool prereqsMet = controller.ArePrereqsMet(def, considerQueued:true);
        if (!prereqsMet) return;

        int available = st.GetAvailablePoints(controller.CostOf);
        if (available < def.cost) controller.NotifyInsufficientPoints();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.UpdatePosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }
}
