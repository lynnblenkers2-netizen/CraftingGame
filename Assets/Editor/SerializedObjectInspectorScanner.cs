using System;
using UnityEngine;
using UnityEditor;

public static class SerializedObjectInspectorScanner
{
    [MenuItem("Tools/Debug/Find SerializedObject Failures In Scene")]
    public static void FindInScene()
    {
        int found = 0;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var comps = root.GetComponentsInChildren<Component>(true);
            foreach (var comp in comps)
            {
                try
                {
                    if (comp == null)
                    {
                        Debug.LogError($"Null component reference found under GameObject '{GetFullPath(root)}'", root);
                        found++;
                        continue;
                    }
                    var so = new SerializedObject(comp);
                    var it = so.GetIterator();
                    while (it.NextVisible(true)) { }
                }
                catch (Exception e)
                {
                    var go = comp != null ? comp.gameObject : root;
                    Debug.LogError($"SerializedObject failure for component '{(comp==null? "<null>" : comp.GetType().FullName)}' on GameObject '{GetFullPath(go)}': {e}", go);
                    found++;
                }
            }
        }

        if (found == 0)
            Debug.Log("No SerializedObject failures detected in the currently open scene.");
        else
            Debug.Log($"Found {found} SerializedObject failure(s) in the scene (see errors). ");
    }

    [MenuItem("Tools/Debug/Find SerializedObject Failures In Project Prefabs")]
    public static void FindInProjectPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int found = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            var comps = prefab.GetComponentsInChildren<Component>(true);
            foreach (var comp in comps)
            {
                try
                {
                    if (comp == null)
                    {
                        Debug.LogError($"Null component inside prefab '{path}'", prefab);
                        found++;
                        continue;
                    }
                    var so = new SerializedObject(comp);
                    var it = so.GetIterator();
                    while (it.NextVisible(true)) { }
                }
                catch (Exception e)
                {
                    Debug.LogError($"SerializedObject failure for component '{(comp==null? "<null>" : comp.GetType().FullName)}' in prefab '{path}': {e}");
                    found++;
                }
            }
        }

        if (found == 0)
            Debug.Log("No SerializedObject failures detected in project prefabs.");
        else
            Debug.Log($"Found {found} SerializedObject failure(s) in project prefabs (see errors).");
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
