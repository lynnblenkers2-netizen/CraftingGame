using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OptionPopup : MonoBehaviour
{
    public static OptionPopup I;

    [Header("Wiring")]
    [FormerlySerializedAs("canvasGroup")]
    public CanvasGroup cg;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI bodyLabel;
    public Transform contentParent;
    public Button closeButton;
    public GameObject optionButtonPrefab;

    Action<int> onSelect;

    void Awake()
    {
        // Singleton robust: erste Instanz gewinnt
        if (I == null) I = this;
        else if (I != this) { Destroy(gameObject); return; }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        HideInstant();
    }

    // ---- NEW: try-find / auto-spawn (Resources fallback) ----
    static bool EnsureInstance()
    {
        if (I != null) return true;

        I = GameObject.FindFirstObjectByType<OptionPopup>(FindObjectsInactive.Include);
        if (I != null) return true;

        var prefab = Resources.Load<OptionPopup>("UI/OptionPopup");
        if (prefab)
        {
            var canvas = GameObject.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas)
            {
                I = Instantiate(prefab, canvas.transform);
                return true;
            }
        }
        return false;
    }

    // Warm up the popup once (pre-instantiates silently so first click is instant)
    static bool warmed;
    public static void Warm()
    {
        if (warmed) return;
        warmed = true;
        if (EnsureInstance() && I != null)
            I.HideInstant();
    }

    public static void Show<T>(string title, IList<T> options, Func<T, string> getLabel, Action<int> onSelectIndex, Func<int, bool> isOptionEnabled = null)
    {
        ShowWithBody(title, null, options, getLabel, onSelectIndex, isOptionEnabled);
    }

    public static void ShowWithBody<T>(string title, string body, IList<T> options, Func<T, string> getLabel, Action<int> onSelectIndex, Func<int, bool> isOptionEnabled = null)
    {
        if (!EnsureInstance())
        {
            Debug.LogWarning("[OptionPopup] Instance missing. " +
                             "Place an OptionPopup in the scene (under Canvas) OR put the prefab at Resources/UI/OptionPopup.");
            return;
        }
        I.InternalShow(title, body, options, getLabel, onSelectIndex, isOptionEnabled);
    }

    void InternalShow<T>(string title, string body, IList<T> options, Func<T, string> getLabel, Action<int> onSelectIndex, Func<int, bool> isOptionEnabled)
    {
        if (options == null || optionButtonPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[OptionPopup] Missing data or wiring.");
            return;
        }

        onSelect = onSelectIndex;

        if (titleLabel) titleLabel.text = string.IsNullOrEmpty(title) ? "Choose" : title;
        if (bodyLabel)
        {
            if (string.IsNullOrEmpty(body))
            {
                bodyLabel.text = "";
                bodyLabel.gameObject.SetActive(false);
            }
            else
            {
                bodyLabel.gameObject.SetActive(true);
                bodyLabel.text = body;
            }
        }

        // Clear children
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        // Build option buttons
        for (int i = 0; i < options.Count; i++)
        {
            var go = Instantiate(optionButtonPrefab, contentParent);
            string text = getLabel != null ? getLabel(options[i]) : options[i]?.ToString();
            int idx = i;
            bool enabled = isOptionEnabled == null || isOptionEnabled(idx);
            if (TryConfigureEntry(go, text, idx, enabled)) continue;

            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);

            if (btn == null)
            {
                Debug.LogError("[OptionPopup] optionButtonPrefab hat keinen Button (auch nicht in Kindern).", go);
                continue;
            }

            if (label) label.text = text;

            btn.interactable = enabled;
            btn.onClick.AddListener(() => HandleSelection(idx));
        }

        Show();
    }

    void Show()
    {
        gameObject.SetActive(true);          // wichtig: aktivieren, falls Prefab inaktiv war
        if (cg)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // Make sure the close button stays hooked up even if prefab listeners were cleared.
        if (closeButton && closeButton.onClick.GetPersistentEventCount() == 0)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void Hide()
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        gameObject.SetActive(false);
        onSelect = null;
    }

    void HideInstant() => Hide();

    bool TryConfigureEntry(GameObject go, string text, int idx, bool interactable)
    {
        var entry = go.GetComponent<OptionPopupButtonUI>() ?? go.GetComponentInChildren<OptionPopupButtonUI>(true);
        if (entry == null) return false;
        entry.Configure(this, text, idx, interactable);
        return true;
    }

    internal void HandleSelection(int idx)
    {
        onSelect?.Invoke(idx);
        Hide();
    }
}
