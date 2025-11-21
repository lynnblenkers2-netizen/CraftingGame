using UnityEditor;
using UnityEngine;

public static class FindMissingScripts
{
    [MenuItem("Tools/Diagnostics/Find Missing Scripts In Scene")]
    public static void FindMissing()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    string path = GetFullPath(go);
                    Debug.LogWarning($"Missing script on '{path}' (component index {i})", go);
                    count++;
                }
            }
        }

        if (count == 0)
            Debug.Log("No missing scripts detected in the currently open scene.");
        else
            Debug.LogWarning($"Found {count} missing script reference(s). Check the Console for details.");
    }

    [MenuItem("Tools/Diagnostics/Find Missing Scripts In Project")]
    public static void FindMissingInProject()
    {
        int count = 0;
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var comps = t.gameObject.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null)
                    {
                        Debug.LogWarning($"Missing script in Prefab: {path} -> {GetFullPath(t.gameObject)} (index {i})");
                        count++;
                    }
                }
            }
        }

        if (count == 0) Debug.Log("No missing scripts detected in project prefabs.");
        else Debug.LogWarning($"Found {count} missing script reference(s) in project prefabs. Check the Console for details.");
    }

    static string GetFullPath(GameObject go)
    {
        return go.transform.parent == null
            ? go.name
            : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
