using UnityEngine;
using System.Collections.Generic;

public class ToastSystem : MonoBehaviour
{
    public static ToastSystem I;

    [Header("Wiring")]
    public RectTransform host;          // ToastHost
    public ToastMessageUI toastPrefab;  // ToastItem.prefab mit ToastMessageUI

    [Header("Behavior")]
    public int maxActive = 3;
    public int maxQueued = 20;
    public bool dontDestroyOnLoad = true;
    public bool coalesceDuplicates = true; // gleiche key -> Zähler

    readonly Queue<ToastRequest> queue = new();
    readonly List<ToastMessageUI> active = new();
    readonly Dictionary<string,int> counters = new(); // key -> count

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (dontDestroyOnLoad)
        {
            if (transform.parent != null) transform.SetParent(null, worldPositionStays: false);
            DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        // aufräumen ausgelaufener Toasts aus 'active'
        for (int i = active.Count - 1; i >= 0; i--)
            if (!active[i]) active.RemoveAt(i);

        // queue abarbeiten
        while (active.Count < maxActive && queue.Count > 0)
        {
            var req = queue.Dequeue();
            Spawn(req);
        }
    }

    void Spawn(ToastRequest r)
    {
        if (!host || !toastPrefab) { Debug.LogWarning("[Toast] Missing host/prefab"); return; }

        var ui = Instantiate(toastPrefab, host);
        ui.Setup(r.type, ComposeTitle(r), r.body);
        ui.Play();
        active.Add(ui);
    }

    string ComposeTitle(ToastRequest r)
    {
        if (!coalesceDuplicates || string.IsNullOrEmpty(r.key)) return r.title;
        int n = 1;
        counters.TryGetValue(r.key, out n);
        return n > 1 ? $"{r.title}  ×{n}" : r.title;
    }

    void Enqueue(ToastRequest r)
    {
        if (coalesceDuplicates && !string.IsNullOrEmpty(r.key))
        {
            counters.TryGetValue(r.key, out int n);
            counters[r.key] = n + 1;
            // Titel der bereits aktiven/queued Toaste mit gleicher key nicht „live“ ändern (einfach halten)
        }

        if (queue.Count >= maxQueued) queue.Dequeue();
        queue.Enqueue(r);
    }

    // ---------- Public API ----------
    public static void Info(string title, string body=null, string key=null, float? showTime=null)
        => Show(ToastType.Info, title, body, key, showTime);
    public static void Success(string title, string body=null, string key=null, float? showTime=null)
        => Show(ToastType.Success, title, body, key, showTime);
    public static void Warning(string title, string body=null, string key=null, float? showTime=null)
        => Show(ToastType.Warning, title, body, key, showTime);
    public static void Error(string title, string body=null, string key=null, float? showTime=null)
        => Show(ToastType.Error, title, body, key, showTime);

    public static void Show(ToastType type, string title, string body=null, string key=null, float? showTime=null)
    {
        if (!I) { Debug.LogWarning("[Toast] No ToastSystem in scene."); return; }
        Debug.Log($"[Toast] {type}: {title} {(string.IsNullOrEmpty(body) ? "" : $"| {body}")}");
        var req = new ToastRequest { type=type, title=title, body=body, key=key, showTime=showTime };
        I.Enqueue(req);
    }

    struct ToastRequest
    {
        public ToastType type;
        public string title;
        public string body;
        public string key;     // gleicher key -> Zähler steigt (×2, ×3,…)
        public float? showTime;
    }
}
