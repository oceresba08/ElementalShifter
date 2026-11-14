using UnityEngine;

public enum ElementFamily { None, Fire, Water, Nature, Earth, Wind, Lightning, Ice }
public enum ElementId { Fire, Water, Nature, Earth, Wind, Paper, Mud, Steam, Bloom, Lightning, Ice }

[CreateAssetMenu(menuName = "Elemental Rumble/Element", fileName = "NewElement")]
public class ElementData : ScriptableObject
{
    public ElementId id;
    public ElementFamily primaryFamily;
    public ElementFamily secondaryFamily = ElementFamily.None;
    [Min(0.1f)] public float speedMultiplier = 1f;
    [Min(0.1f)] public float defenseMultiplier = 1f;
    [Min(0.1f)] public float attackMultiplier = 1f;
    public string skillName = "Elemental Pulse";
    public string ultimateName = "Elemental Burst";
    [TextArea] public string description;
    public Material appearanceMaterial;
    // Optional shape change: a crumpled-paper sphere, crystal ball, mud lump, etc.
    public GameObject visualPrefab;
    public GameObject loopingAuraPrefab;
}
