using UnityEngine;
public enum ElementFamily { None, Fire, Water, Nature, Earth, Wind, Lightning, Ice }
public enum ElementId { Fire, Water, Nature, Earth, Wind, Paper, Mud, Steam, Bloom, Lightning, Ice }
public enum ElementClass { Assassin, Fighter, Heavy, Ranger, Healer, Trickster, Tank, Mage, Support, Burst, Controller }
[CreateAssetMenu(menuName = "Elemental Shift/Element", fileName = "NewElement")]
public class ElementData : ScriptableObject
{
    public ElementId id;
    public ElementFamily primaryFamily;
    public ElementFamily secondaryFamily = ElementFamily.None;
    public ElementClass combatClass = ElementClass.Fighter;
    [Min(.1f)] public float speedMultiplier = 1f;
    [Min(.1f)] public float defenseMultiplier = 1f;
    [Min(.1f)] public float attackMultiplier = 1f;
    [Min(1f)] public float enemyLayerHealth = 35f;
    public string skillName = "Elemental Pulse";
    public string ultimateName = "Elemental Burst";
    [TextArea] public string description;
    public Material appearanceMaterial;
    public GameObject visualPrefab;
    public GameObject loopingAuraPrefab;
}
