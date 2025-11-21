using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ActorsPanelUI : MonoBehaviour
{
    public ActorManager manager;
    public Transform listParent;
    public GameObject actorCardPrefab;
    public ActorCraftingPanelUI craftingPanel;
    [Header("Scrolling")]
    public ScrollRect scrollRect;
    [Tooltip("If true, logs all ScrollRects under 'ActorsDock' and their content at startup to diagnose missing content references.")]
    public bool logScrollRectsOnAwake = false;
    [Header("Task Catalog")]
    public ActorTaskCatalog catalog;
    public bool autoPopulateFromCatalog = true;

    [Header("Task Catalogs")]
    public List<ForageArea> forageAreas = new();
    public List<SellRoute> sellRoutes = new();
    public List<ResearchDomain> researchDomains = new();
    public List<CraftPlan> craftPlans = new();
    public List<TavernVisit> tavernVisits = new();

    readonly List<ActorCardUI> cards = new();

    void Awake()
    {
        EnsureScrollContent();
        if (logScrollRectsOnAwake) DebugLogScrollRects();
        ApplyCatalogIfNeeded();
    }
    void OnValidate()
    {
        EnsureScrollContent();
    }
    void OnEnable() { Rebuild(); }
    void OnDisable() { Clear(); }
    void LateUpdate()
    {
        // Safety: if something nuls the ScrollRect content (e.g., prefab overrides, inactive toggles), reassign.
        if (scrollRect != null && scrollRect.content == null && listParent != null)
        {
            var rt = listParent as RectTransform;
            if (rt != null)
                scrollRect.content = rt;
        }
    }

    public void Rebuild()
    {
        Clear();
        if (!manager || !actorCardPrefab || !listParent) return;
        EnsureScrollContent();

        var global = GameObject.FindFirstObjectByType<GlobalInventoryService>(FindObjectsInactive.Include);
        foreach (var a in manager.actors)
        {
            if (a == null) continue;
            var go = Instantiate(actorCardPrefab, listParent);
            var card = go.GetComponent<ActorCardUI>();
            if (!card) continue;
            if (global) card.SetServices(global);
            card.Bind(a, this);
            cards.Add(card);
        }
    }

    void Clear()
    {
        if (listParent)
        {
            for (int i = listParent.childCount - 1; i >= 0; i--)
            {
                Destroy(listParent.GetChild(i).gameObject);
            }
        }
        cards.Clear();
    }

    public void AssignForage(ActorInstance a, int areaIndex, bool repeat)
    {
        if (!manager || areaIndex < 0 || areaIndex >= forageAreas.Count) return;
        manager.AssignForage(a, forageAreas[areaIndex], repeat);
    }

    public void AssignSell(ActorInstance a, int routeIndex, bool repeat)
    {
        if (!manager || routeIndex < 0 || routeIndex >= sellRoutes.Count) return;
        manager.AssignSell(a, sellRoutes[routeIndex], repeat);
    }

    public void AssignResearch(ActorInstance a, int idx, bool repeat)
    {
        if (!manager || idx < 0 || idx >= researchDomains.Count) return;
        manager.AssignResearch(a, researchDomains[idx], repeat);
    }

    public void AssignCraft(ActorInstance a, int idx, bool repeat)
    {
        if (!manager || idx < 0 || idx >= craftPlans.Count) return;
        manager.AssignCraft(a, craftPlans[idx], repeat);
    }

    public void AssignTavern(ActorInstance a, int idx, bool repeat)
    {
        if (!manager || idx < 0 || idx >= tavernVisits.Count) return;
        manager.AssignTavernVisit(a, tavernVisits[idx], repeat);
    }

    public void ApplyCatalog(ActorTaskCatalog source, bool rebuildNow = false)
    {
        if (!source) return;
        catalog = source;
        CopyList(forageAreas, source.forageAreas);
        CopyList(sellRoutes, source.sellRoutes);
        CopyList(researchDomains, source.researchDomains);
        CopyList(craftPlans, source.craftPlans);
        CopyList(tavernVisits, source.tavernVisits);
        if (rebuildNow && isActiveAndEnabled) Rebuild();
    }

    void ApplyCatalogIfNeeded()
    {
        if (autoPopulateFromCatalog && catalog != null)
            ApplyCatalog(catalog, false);
    }

    static void CopyList<T>(List<T> target, List<T> source)
    {
        if (target == null) return;
        target.Clear();
        if (source != null) target.AddRange(source);
    }

    void EnsureScrollContent()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (scrollRect == null) return;

        // If listParent not set, try to use existing content on scrollRect
        if (listParent == null && scrollRect.content != null)
            listParent = scrollRect.content;

        if (listParent == null) return;

        var rt = listParent as RectTransform;
        if (rt != null && scrollRect.content != rt)
            scrollRect.content = rt;
    }

    void DebugLogScrollRects()
    {
        var root = GameObject.Find("ActorsDock");
        if (root == null)
        {
            Debug.Log("[ActorsPanelUI] DebugLogScrollRects: ActorsDock not found in scene.");
            return;
        }

        var rects = root.GetComponentsInChildren<ScrollRect>(true);
        if (rects == null || rects.Length == 0)
        {
            Debug.Log("[ActorsPanelUI] DebugLogScrollRects: no ScrollRects found under ActorsDock.");
            return;
        }

        foreach (var sr in rects)
        {
            if (sr == null) continue;
            string path = GetPath(sr.transform);
            string contentName = sr.content ? sr.content.name : "<null>";
            Debug.Log($"[ActorsPanelUI] ScrollRect path={path} content={contentName}", sr);
        }
    }

    string GetPath(Transform t)
    {
        if (t == null) return "<null>";
        var names = new System.Collections.Generic.List<string>();
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }
}
