#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class RecipeDatabaseBuilder
{
    [MenuItem("Tools/Recipes/Build Recipe Database")]
    public static void BuildRecipeDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:ShapedRecipe");
        var list = new List<ShapedRecipe>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var r = AssetDatabase.LoadAssetAtPath<ShapedRecipe>(path);
            if (r != null) list.Add(r);
        }

        var dbPath = "Assets/ScriptableObjects/RecipeDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(dbPath);
        if (db == null)
        {
            var folder = System.IO.Path.GetDirectoryName(dbPath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            db = ScriptableObject.CreateInstance<RecipeDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // replace contents
        db.recipes = list;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Also ensure a copy exists in Assets/Resources so runtime builds can load it via Resources.Load
        try
        {
            var resPath = "Assets/Resources/RecipeDatabase.asset";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var resDb = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(resPath);
            if (resDb == null)
            {
                resDb = ScriptableObject.CreateInstance<RecipeDatabase>();
                AssetDatabase.CreateAsset(resDb, resPath);
            }
            resDb.recipes = list;
            EditorUtility.SetDirty(resDb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RecipeDatabase built and written to {dbPath} and {resPath} ({list.Count} recipes)");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("RecipeDatabaseBuilder: failed to write Resources copy: " + ex);
        }

        Debug.Log($"RecipeDatabase built: {list.Count} recipes written to {dbPath}");
    }
}
#endif
