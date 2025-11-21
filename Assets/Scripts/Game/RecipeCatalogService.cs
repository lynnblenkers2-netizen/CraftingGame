using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene service that stores the player's discovered recipes.
/// Actors call AddRecipe when they discover one. UI can subscribe to OnRecipeAdded.
/// Place one instance in the scene (e.g. in a "GameServices" root) or it will create a transient one at runtime.
/// </summary>
public class RecipeCatalogService : MonoBehaviour
{
    public static RecipeCatalogService Instance { get; private set; }

    [SerializeField] private List<ShapedRecipe> discovered = new();
    [Header("Startup / Seeding")]
    [Tooltip("Recipes to mark as known to the player at game start. Leave empty if no starter recipes should be pre-known.")]
    [SerializeField] private List<ShapedRecipe> starterRecipes = new();
    [Tooltip("If enabled, attempt to seed ALL recipes from a central RecipeDatabase. Disabled by default to avoid showing unrevealed recipes in the catalog.")]
    [SerializeField] private bool seedAllFromDatabase = false;
    [Header("Editor Notes")]
    [TextArea(3,6)]
    [SerializeField] public string editorNote = "RecipeCatalogService stores the player's discovered recipes at runtime.\nUse 'starterRecipes' to configure which recipes are pre-known.\nTo import all project recipes into a central database, use Tools/Recipes/Build Recipe Database and enable 'seedAllFromDatabase' (not recommended if you want discovery).\nRecipe discoveries are still propagated to CraftingManager instances at runtime.";

    public event Action<ShapedRecipe> OnRecipeAdded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If no discovered recipes yet, try seeding from a project-wide RecipeDatabase (preferred), then
        // fallback to seeding from CraftingManager instances in the scene.
        try
        {
            if ((discovered == null || discovered.Count == 0))
            {
                // 1) If explicit starter recipes are provided in the inspector, seed those only.
                if (starterRecipes != null && starterRecipes.Count > 0)
                {
                    Seed(starterRecipes);
                    Debug.Log($"RecipeCatalogService: seeded {starterRecipes.Count} starter recipe(s) from inspector.");
                }
                else if (seedAllFromDatabase)
                {
                    bool seeded = false;

                    // 2) Runtime-friendly: check for a RecipeDatabase in Resources/RecipeDatabase
                    try
                    {
                        var resDb = Resources.Load<RecipeDatabase>("RecipeDatabase");
                        if (resDb != null && resDb.recipes != null && resDb.recipes.Count > 0)
                        {
                            Seed(resDb.recipes);
                            seeded = true;
                            Debug.Log($"RecipeCatalogService: seeded {resDb.recipes.Count} recipes from Resources/RecipeDatabase");
                        }
                    }
                    catch { }

                    // 3) Editor-only: try loading the canonical RecipeDatabase asset path
#if UNITY_EDITOR
                    if (!seeded)
                    {
                        try
                        {
                            var dbPath = "Assets/ScriptableObjects/RecipeDatabase.asset";
                            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeDatabase>(dbPath);
                            if (db != null && db.recipes != null && db.recipes.Count > 0)
                            {
                                Seed(db.recipes);
                                seeded = true;
                                Debug.Log($"RecipeCatalogService: seeded {db.recipes.Count} recipes from {dbPath}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("RecipeCatalogService: failed to seed from RecipeDatabase asset: " + ex);
                        }
                    }
#endif

                    if (!seeded)
                        Debug.Log("RecipeCatalogService: seedAllFromDatabase set but no RecipeDatabase found.");
                }
                else
                {
                    // No auto-seeding configured: do not add project recipes to 'discovered'.
                    Debug.Log("RecipeCatalogService: no starter recipes configured and seedAllFromDatabase=false; catalog will start empty.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("RecipeCatalogService: seeding failed: " + ex);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IReadOnlyList<ShapedRecipe> GetAll() => discovered.AsReadOnly();

    /// Add recipe if not already known. Returns true when newly added.
    public bool AddRecipe(ShapedRecipe r)
    {
        if (r == null)
        {
            Debug.LogWarning("RecipeCatalogService.AddRecipe called with null recipe");
            return false;
        }
        Debug.Log($"RecipeCatalogService.AddRecipe: attempting to add {r.name}");
        if (discovered.Contains(r))
        {
            Debug.Log($"RecipeCatalogService.AddRecipe: recipe {r.name} is already known");
            return false;
        }
        discovered.Add(r);
        Debug.Log($"RecipeCatalogService.AddRecipe: recipe {r.name} added to catalog (count={discovered.Count})");
        try { OnRecipeAdded?.Invoke(r); } catch (Exception ex) { Debug.LogWarning("OnRecipeAdded handler threw: " + ex); }

        // Also propagate the discovered recipe into any CraftingManager instances
        try
        {
            var mgrs = FindObjectsOfType<CraftingManager>();
            Debug.Log($"RecipeCatalogService.AddRecipe: propagating {r.name} to {mgrs.Length} CraftingManager(s)");
            int propagated = 0;
            foreach (var m in mgrs)
            {
                if (m == null) continue;
                m.AddRuntimeRecipe(r);
                propagated++;
            }
            Debug.Log($"RecipeCatalogService.AddRecipe: propagated to {propagated} CraftingManager(s)");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("RecipeCatalogService: failed to propagate recipe to CraftingManager: " + ex);
        }
        return true;
    }

    public bool HasRecipe(ShapedRecipe r) => r != null && discovered.Contains(r);

    // Optional: helper to seed starting recipes
    public void Seed(IEnumerable<ShapedRecipe> list)
    {
        if (list == null) return;
        foreach (var r in list) AddRecipe(r);
    }

    // Debug helper: prints known recipes (names, instance IDs and editor paths when available)
    public void DebugDumpKnownRecipes()
    {
        Debug.Log($"RecipeCatalogService: Known recipes ({discovered?.Count ?? 0}):");
        if (discovered == null) return;
        foreach (var r in discovered)
        {
            if (r == null) { Debug.Log(" - <null>"); continue; }
            string s = $" - {r.name} (id={r.GetInstanceID()})";
#if UNITY_EDITOR
            try
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(r);
                if (!string.IsNullOrEmpty(path)) s += $" path={path}";
            }
            catch { }
#endif
            Debug.Log(s);
        }
    }
}
