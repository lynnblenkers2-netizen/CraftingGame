Recipe Catalog — Quick Notes

Purpose
- The `RecipeCatalogService` is the runtime store of recipes the player has discovered.
- `CraftingManager` instances contain the recipes a particular crafting UI offers.
- `ShapedRecipe` ScriptableObjects are the authoritative recipe assets in the project.

Developer workflow
1) Build a central database (optional but recommended)
   - In the Editor: Tools → Recipes → Build Recipe Database
   - This creates/updates `Assets/ScriptableObjects/RecipeDatabase.asset` and also writes a runtime copy to `Assets/Resources/RecipeDatabase.asset`.
   - The builder scans the project for `ShapedRecipe` assets and stores references in the database.

2) Configure what the player knows at start
   - Select the `RecipeCatalogService` in your scene (create one if needed).
   - Use the `Starter Recipes` list to put recipes the player should initially know.
   - Keep `Seed All From Database` OFF if you want discovery to matter (default) — turning it on will populate the catalog with every recipe from the `RecipeDatabase`.

3) Discovery flow at runtime
   - Actors run research tasks and may `UnlockRecipe(ShapedRecipe r)`.
   - `ActorManager` calls `RecipeCatalogService.AddRecipe(r)` — the service adds the recipe to the runtime catalog and raises `OnRecipeAdded`.
   - The `RecipeCatalogUI` subscribes to `RecipeCatalogService` and will update when recipes are added.
   - Discovered recipes are propagated into `CraftingManager` instances at runtime so they become craftable immediately.

Notes & Recommendations
- Keep your canonical `ShapedRecipe` assets organized under `Assets/ScriptableObjects/Recipes/` (or similar). The builder finds them project-wide.
- To persist player discoveries across sessions, implement a save/load that serializes discovered recipe asset paths or IDs (not provided by default).
- `RecipeDatabase` is optional. If you prefer a single authoring list for designers, use the builder to keep the database up to date.

Troubleshooting
- If a discovered recipe doesn't appear in the catalog:
  - Open the Console and look for debug logs from `ActorManager`, `RecipeCatalogService`, and `CraftingManager`.
  - Verify the discovered `ShapedRecipe` is the same asset instance as the one in your project (matching asset path).

Contact
- If you want, I can add automatic save/load for discoveries, or expose editor buttons to seed/clear the runtime catalog during testing.
