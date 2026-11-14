using System.Collections;
using UnityEngine;

/// <summary>
/// Tiny procedural VFX library for Phase 4. No imported art assets are required.
/// Attach effects at runtime. To replace the look later, swap in authored prefab effects.
/// </summary>
public static class ElementalVFX
{
    static Material particleMaterial;

    public static Color ColorFor(ElementData data)
    {
        if (data == null) return new Color(0.65f, 0.8f, 1f);
        switch (data.id)
        {
            case ElementId.Fire: return new Color(1f, 0.31f, 0.08f);
            case ElementId.Water: return new Color(0.12f, 0.64f, 1f);
            case ElementId.Nature: return new Color(0.18f, 0.95f, 0.38f);
            case ElementId.Earth: return new Color(0.66f, 0.43f, 0.22f);
            case ElementId.Wind: return new Color(0.27f, 0.96f, 0.84f);
            case ElementId.Paper: return new Color(0.95f, 0.88f, 0.67f);
            case ElementId.Mud: return new Color(0.46f, 0.34f, 0.22f);
            case ElementId.Steam: return new Color(0.76f, 0.87f, 0.94f);
            case ElementId.Bloom: return new Color(0.96f, 0.46f, 0.76f);
            case ElementId.Lightning: return new Color(1f, 0.89f, 0.19f);
            case ElementId.Ice: return new Color(0.54f, 0.91f, 1f);
            default: return Color.white;
        }
    }

    static Material GetMaterial()
    {
        if (particleMaterial != null) return particleMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) { Debug.LogWarning("No suitable particle shader found."); return null; }
        particleMaterial = new Material(shader) { name = "Phase4_RuntimeParticleMaterial" };
        if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
        if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", Color.white);
        return particleMaterial;
    }

    public static ParticleSystem Make(string name, Transform parent, Vector3 position,
        Color color, bool loop, int count, float radius, float lifetime, float speed,
        float size, float rate = 0f)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) { obj.transform.SetParent(parent, false); obj.transform.localPosition = position; }
        else obj.transform.position = position;
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = Mathf.Max(0.15f, lifetime);
        main.loop = loop;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = Mathf.Max(50, count * 4);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = loop ? Mathf.Max(1, rate) : 0;
        if (!loop) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 400)) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        colorLife.color = new ParticleSystem.MinMaxGradient(new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(0f, 1f) }
        });
        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, .28f), new Keyframe(.3f, 1f), new Keyframe(1f, 0f)));
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 4;
        renderer.sharedMaterial = GetMaterial();
        ps.Play();
        if (!loop) Object.Destroy(obj, lifetime + 1.1f);
        return ps;
    }

    public static void Burst(string label, Vector3 worldPos, Color color, int count = 26,
        float radius = .25f, float speed = 4f, float size = .19f)
    {
        Make(label, null, worldPos, color, false, count, radius, .65f, speed, size);
    }

    public static ParticleSystem Aura(string label, Transform target, Color color,
        float radius = .6f, float emissionRate = 14f, float size = .16f)
    {
        return Make(label, target, Vector3.zero, color, true, 18, radius, .7f, .65f, size, emissionRate);
    }
}
