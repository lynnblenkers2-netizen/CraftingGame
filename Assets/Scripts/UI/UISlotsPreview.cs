using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Preview temporärer Slots im Editor (keine Runtime-Objekte; Playmode räumt auf)
[ExecuteAlways]
public class UISlotsPreview : MonoBehaviour
{
    public enum CountMode { Fixed, FromInventoryCapacity }

    [Header("Setup")]
    public Transform slotParent;        // meist dein Panel mit GridLayoutGroup
    public GameObject slotPrefab;       // dein Slot-Prefab
    public CountMode countMode = CountMode.Fixed;
    public int fixedCount = 16;         // z.B. 16 fürs Crafting
    public GlobalInventoryService inventoryService;   // nur für FromInventoryCapacity

    [Header("Preview")]
    public bool showPreviewInEditMode = true;
    public Sprite placeholderIcon;      // optional hübsches Dummy-Icon
    public bool disableInteractiveComponents = true;

#if UNITY_EDITOR
    const string PreviewName = "Slot (Preview)";
    bool pendingRebuild;   // OnValidate -> in Update ausführen
    bool wasPlaying;
#endif

    void Reset() { slotParent = transform; }

    void OnEnable()
    {
#if UNITY_EDITOR
        RequestRebuild();
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        ClearPreview();
#endif
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            if (!wasPlaying) { wasPlaying = true; ClearPreview(); }
            return;
        }
        wasPlaying = false;

        if (!showPreviewInEditMode)
        {
            if (CountPreviewChildren() > 0) ClearPreview();
            return;
        }

        if (pendingRebuild)
        {
            pendingRebuild = false;
            RebuildNow();
        }
#endif
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        // NUR markieren; kein Erzeugen/Löschen hier (Unity verbietet AddComponent/Instantiate)
        RequestRebuild();
#endif
    }

#if UNITY_EDITOR
    void RequestRebuild() => pendingRebuild = true;

    void RebuildNow()
    {
        if (!slotParent || !slotPrefab) return;

        int want = (countMode == CountMode.FromInventoryCapacity && inventoryService != null)
                 ? Mathf.Max(0, inventoryService.startingCapacity)
                 : Mathf.Max(0, fixedCount);

        int have = CountPreviewChildren();

        if (have > want) RemovePreviewChildren(have - want);
        else if (have < want) AddPreviewChildren(want - have);
        // sonst passt's
    }

    int CountPreviewChildren()
    {
        int c = 0;
        if (!slotParent) return 0;
        foreach (Transform t in slotParent)
            if (t && t.name == PreviewName) c++;
        return c;
    }

    void RemovePreviewChildren(int count)
    {
        for (int i = slotParent.childCount - 1; i >= 0 && count > 0; i--)
        {
            var child = slotParent.GetChild(i);
            if (!child || child.name != PreviewName) continue;
            Undo.DestroyObjectImmediate(child.gameObject);
            count--;
        }
    }

    void AddPreviewChildren(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Prefab-instantiate im Editor
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, slotParent);
            if (!go) go = (GameObject)Object.Instantiate(slotPrefab, slotParent);

            go.name = PreviewName;
            go.hideFlags |= HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            if (disableInteractiveComponents)
            {
                // Deaktiviert ItemSlotUI und gängige UI-Interaktionen
                var slotUI = go.GetComponent<ItemSlotUI>();
                if (slotUI) slotUI.enabled = false;
                foreach (var sel in go.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                    sel.enabled = false;
                foreach (var g in go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    g.raycastTarget = false;
                foreach (var gr in go.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true))
                    gr.enabled = false;
                foreach (var et in go.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true))
                    et.enabled = false;
            }

            // Optional: Dummy-Icon sichtbar machen
            if (placeholderIcon)
            {
                var icon = go.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon) { icon.sprite = placeholderIcon; icon.enabled = true; }
                var countObj = go.transform.Find("Count");
                if (countObj) countObj.gameObject.SetActive(false);
            }

            Undo.RegisterCreatedObjectUndo(go, "Add Slot Preview");
        }
    }

    [ContextMenu("Preview ▶ Rebuild Now")]
    void CM_Rebuild() { pendingRebuild = false; ClearPreview(); RebuildNow(); }

    [ContextMenu("Preview ✖ Clear")]
    void CM_Clear() { ClearPreview(); }

    public void ClearPreview()
    {
        if (!slotParent) return;
        for (int i = slotParent.childCount - 1; i >= 0; i--)
        {
            var child = slotParent.GetChild(i);
            if (child && child.name == PreviewName)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
#endif
}
