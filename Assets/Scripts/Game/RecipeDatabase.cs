using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Recipe Database", fileName = "RecipeDatabase")]
public class RecipeDatabase : ScriptableObject
{
    public List<ShapedRecipe> recipes = new List<ShapedRecipe>();
}
