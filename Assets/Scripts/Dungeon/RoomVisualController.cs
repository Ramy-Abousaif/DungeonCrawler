using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomVisualState
{
    Hidden,
    Dark,
    Adjacent,
    Active
}

public class RoomVisualController : MonoBehaviour
{
    public DungeonRoom roomData;
    
    private Renderer[] renderers;
    private Light[] lights;

    private Dictionary<Light, float> originalIntensities = new();
    private Dictionary<Renderer, Color> originalBaseColors = new();

    private Dictionary<Renderer, MaterialPropertyBlock> propertyBlocks = new();

    [Header("Multipliers")]
    public float activeMultiplier = 1f;
    public float adjacentMultiplier = 0.7f;
    public float darkMultiplier = 0.4f;

    public float transitionDuration = 0.4f;

    private Coroutine transitionRoutine;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    public void AssignLightsAndRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        lights = GetComponentsInChildren<Light>(true);

        // Cache light intensities
        foreach (var l in lights)
        {
            originalIntensities[l] = l.intensity;
        }

        // Cache base colors
        foreach (var r in renderers)
        {
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);

            Color baseColor = Color.white;

            if (r.sharedMaterial.HasProperty(BaseColorID))
            {
                baseColor = r.sharedMaterial.GetColor(BaseColorID);
            }

            originalBaseColors[r] = baseColor;
            propertyBlocks[r] = block;
        }
    }

    public void SetState(RoomVisualState state)
    {
        if (renderers == null || renderers.Length == 0)
        {
            AssignLightsAndRenderers();
        }
        float multiplier = state switch
        {
            RoomVisualState.Active => activeMultiplier,
            RoomVisualState.Adjacent => adjacentMultiplier,
            RoomVisualState.Dark => darkMultiplier,
            RoomVisualState.Hidden => 0f,
            _ => 1f
        };

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(LerpVisuals(multiplier));
    }

    IEnumerator LerpVisuals(float multiplier)
    {
        if (this == null) yield break;
        if (!gameObject.activeInHierarchy) yield break;
        float time = 0f;

        Dictionary<Light, float> startLightValues = new();
        Dictionary<Renderer, Color> startColorValues = new();

        foreach (var l in lights)
        {
            if (l == null) continue;
            startLightValues[l] = l.intensity;            
        }

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var block = propertyBlocks[r];
            r.GetPropertyBlock(block);

            if (block.HasColor(BaseColorID))
                startColorValues[r] = block.GetColor(BaseColorID);
            else
                startColorValues[r] = originalBaseColors[r];
        }

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = time / transitionDuration;

            // Lerp lights
            foreach (var l in lights)
            {
                float target = originalIntensities[l] * multiplier;
                l.intensity = Mathf.Lerp(startLightValues[l], target, t);
            }

            // Lerp materials
            foreach (var r in renderers)
            {
                Color original = originalBaseColors[r];
                Color target = original * multiplier;

                Color lerped = Color.Lerp(startColorValues[r], target, t);

                var block = propertyBlocks[r];
                block.SetColor(BaseColorID, lerped);
                r.SetPropertyBlock(block);
            }

            yield return null;
        }
    }

    public Color GetRendererColor(Renderer r)
    {
        if (!propertyBlocks.TryGetValue(r, out var block))
            block = new MaterialPropertyBlock();

        r.GetPropertyBlock(block);
        return block.GetColor(BaseColorID);
    }

    public void FadeRenderer(Renderer r, float alpha)
    {
        if (!propertyBlocks.TryGetValue(r, out var block))
            block = new MaterialPropertyBlock();

        r.GetPropertyBlock(block);
        Color c = block.GetColor(BaseColorID);
        c.a = alpha;
        block.SetColor(BaseColorID, c);
        r.SetPropertyBlock(block);
    }
}