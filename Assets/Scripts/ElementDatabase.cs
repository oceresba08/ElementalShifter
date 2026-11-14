using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElementRecipe
{
    public ElementData ingredientA;
    public ElementData ingredientB;
    public ElementData result;
}

[CreateAssetMenu(menuName = "Elemental Shift/Element Database", fileName = "ElementDatabase")]
public class ElementDatabase : ScriptableObject
{
    public ElementData[] allElements;
    public ElementRecipe[] recipes;

    public bool TryFuse(ElementData a, ElementData b, out ElementData result)
    {
        result = null;
        if (a == null || b == null || recipes == null) return false;

        foreach (ElementRecipe recipe in recipes)
        {
            if (recipe == null || recipe.result == null) continue;
            bool match = (recipe.ingredientA == a && recipe.ingredientB == b) ||
                         (recipe.ingredientB == a && recipe.ingredientA == b);
            if (match)
            {
                result = recipe.result;
                return true;
            }
        }
        return false;
    }

    public IEnumerable<ElementData> Elements => allElements ?? Array.Empty<ElementData>();
}
