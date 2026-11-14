using UnityEngine;

public static class ElementMatchups
{
    // Main cycle: Fire > Nature > Earth > Wind > Water > Fire.
    public static float FamilyMultiplier(ElementFamily attacker, ElementFamily defender)
    {
        if (attacker == ElementFamily.None || defender == ElementFamily.None) return 1f;
        if ((attacker == ElementFamily.Fire && defender == ElementFamily.Nature) ||
            (attacker == ElementFamily.Nature && defender == ElementFamily.Earth) ||
            (attacker == ElementFamily.Earth && defender == ElementFamily.Wind) ||
            (attacker == ElementFamily.Wind && defender == ElementFamily.Water) ||
            (attacker == ElementFamily.Water && defender == ElementFamily.Fire) ||
            (attacker == ElementFamily.Lightning && defender == ElementFamily.Water) ||
            (attacker == ElementFamily.Earth && defender == ElementFamily.Lightning) ||
            (attacker == ElementFamily.Fire && defender == ElementFamily.Ice) ||
            (attacker == ElementFamily.Ice && defender == ElementFamily.Nature))
            return 1.6f;

        if ((attacker == ElementFamily.Nature && defender == ElementFamily.Fire) ||
            (attacker == ElementFamily.Earth && defender == ElementFamily.Nature) ||
            (attacker == ElementFamily.Wind && defender == ElementFamily.Earth) ||
            (attacker == ElementFamily.Water && defender == ElementFamily.Wind) ||
            (attacker == ElementFamily.Fire && defender == ElementFamily.Water) ||
            (attacker == ElementFamily.Water && defender == ElementFamily.Lightning) ||
            (attacker == ElementFamily.Lightning && defender == ElementFamily.Earth) ||
            (attacker == ElementFamily.Ice && defender == ElementFamily.Fire) ||
            (attacker == ElementFamily.Nature && defender == ElementFamily.Ice))
            return 0.65f;

        return 1f;
    }

    public static float Advantage(ElementData attacker, ElementData defender)
    {
        if (attacker == null || defender == null) return 1f;

        float vsPrimary = FamilyMultiplier(attacker.primaryFamily, defender.primaryFamily);
        if (defender.secondaryFamily != ElementFamily.None)
            vsPrimary = Mathf.Lerp(vsPrimary,
                FamilyMultiplier(attacker.primaryFamily, defender.secondaryFamily), 0.3f);

        if (attacker.secondaryFamily == ElementFamily.None) return vsPrimary;

        float secondary = FamilyMultiplier(attacker.secondaryFamily, defender.primaryFamily);
        if (defender.secondaryFamily != ElementFamily.None)
            secondary = Mathf.Lerp(secondary,
                FamilyMultiplier(attacker.secondaryFamily, defender.secondaryFamily), 0.3f);

        return Mathf.Lerp(vsPrimary, secondary, 0.35f);
    }
}
