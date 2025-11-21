using System;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectionOnErrorLogger
{
    static bool enabled = false;

    static SelectionOnErrorLogger()
    {
        // start disabled; user toggles via menu
    }

    [MenuItem("Tools/Debug/Toggle Selection-On-Error Logger")]
    public static void Toggle()
    {
        enabled = !enabled;
        if (enabled)
        {
            Application.logMessageReceived += OnLog;
            Debug.Log("Selection-On-Error Logger ENABLED");
        }
        else
        {
            Application.logMessageReceived -= OnLog;
            Debug.Log("Selection-On-Error Logger DISABLED");
        }
    }

    [MenuItem("Tools/Debug/Dump Current Selection")] 
    public static void DumpSelectionNow()
    {
        DumpSelection("Manual dump");
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        // look for the editor exceptions we observed
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (condition.Contains("SerializedObjectNotCreatableException") || condition.Contains("GameObjectInspector.OnDisable") || stackTrace.Contains("GameObjectInspector.OnDisable") || stackTrace.Contains("Editor.CreateSerializedObject") || stackTrace.Contains("UnityEditor.UI.ImageEditor.OnEnable"))
            {
                DumpSelection(condition + "\n" + stackTrace);
            }
        }
    }

    static void DumpSelection(string triggerInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Selection Dump Triggered ===");
        sb.AppendLine("Trigger: " + triggerInfo);
        var sel = Selection.objects;
        sb.AppendLine($"Selection.count = {sel.Length}");
        for (int i = 0; i < sel.Length; i++)
        {
            var o = sel[i];
            if (o == null)
            {
                sb.AppendLine($"[{i}] NULL (Unity reports null)");
                continue;
            }
            var go = o as GameObject;
            if (go != null)
            {
                sb.AppendLine($"[{i}] GameObject: '{GetFullPath(go)}' (path asset: {AssetDatabase.GetAssetPath(go)})");
                var comps = go.GetComponents<Component>();
                for (int c = 0; c < comps.Length; c++)
                {
                    var comp = comps[c];
                    if (comp == null)
                        sb.AppendLine($"    [{c}] Missing component (null)");
                    else
                        sb.AppendLine($"    [{c}] {comp.GetType().FullName}");
                }
            }
            else
            {
                sb.AppendLine($"[{i}] Object: '{o.name}' Type: {o.GetType().FullName} assetPath: {AssetDatabase.GetAssetPath(o)}");
            }
        }

        // also dump selection via Selection.instanceIDs to detect stale ids
        var ids = Selection.instanceIDs;
        sb.AppendLine($"Selection.instanceIDs length = {ids.Length}");
        for (int i = 0; i < ids.Length; i++)
            sb.AppendLine($"  id[{i}] = {ids[i]}");

        sb.AppendLine("=== End Selection Dump ===");
        Debug.Log(sb.ToString());
    }

    static string GetFullPath(GameObject go)
    {
        if (go == null) return "<null>";
        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
