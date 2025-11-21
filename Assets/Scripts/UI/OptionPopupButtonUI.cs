using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionPopupButtonUI : MonoBehaviour, IPointerClickHandler
{
    public Button button;
    public TextMeshProUGUI label;
    public UnityEvent extraClickEvents;

    OptionPopup owner;
    int index;

    void Awake() => EnsureButton();

    void OnDisable()
    {
        if (button) button.onClick.RemoveListener(OnButtonClicked);
    }

    public void Configure(OptionPopup popup, string text, int idx, bool interactable = true)
    {
        owner = popup;
        index = idx;
        if (label) label.text = text ?? string.Empty;
        EnsureButton();
        if (!button) Debug.LogWarning("[OptionPopupButtonUI] Button reference missing.", this);
        if (button) button.interactable = interactable;
        if (label) label.alpha = interactable ? 1f : 0.5f;
    }

    void EnsureButton()
    {
        if (!button) button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        if (!button) return;
        button.onClick.RemoveListener(OnButtonClicked);
        button.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        extraClickEvents?.Invoke();
        owner?.HandleSelection(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // no-op; interface retained for future diagnostics if needed
    }
}
