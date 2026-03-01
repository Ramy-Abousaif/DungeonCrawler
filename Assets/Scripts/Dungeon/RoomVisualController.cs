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

    // List to hold WallSegment references
    private WallSegment[] wallSegments;

    private Light[] lights;

    private Dictionary<Light, float> originalIntensities = new();
    private Dictionary<WallSegment, float> originalWallAlphas = new();

    private Dictionary<WallSegment, MaterialPropertyBlock> wallPropertyBlocks = new();

    [Header("Multipliers")]
    public float activeMultiplier = 1f;
    public float adjacentMultiplier = 0.7f;
    public float darkMultiplier = 0.4f;

    public float transitionDuration = 0.4f;

    private Coroutine transitionRoutine;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    public void AssignWallSegmentsAndLights()
    {
        // Find all WallSegment components in the children of this room
        wallSegments = GetComponentsInChildren<WallSegment>(true);
        lights = GetComponentsInChildren<Light>(true);

        // Cache the original alpha and color for each wall segment
        foreach (var segment in wallSegments)
        {
            // Get the material from the first renderer in the segment
            Renderer renderer = segment.GetComponentInChildren<Renderer>();

            // Create a MaterialPropertyBlock to hold the properties
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            // Check and cache the original color (RGB) and alpha separately
            Color originalColor = renderer.material.GetColor(BaseColorID);
            originalWallAlphas[segment] = originalColor.a; // Store original alpha

            // also make sure the property block is populated with that base color
            // so future GetPropertyBlock() calls will return the correct value
            block.SetColor(BaseColorID, originalColor);
            renderer.SetPropertyBlock(block);

            // Store the material property block to modify later
            wallPropertyBlocks[segment] = block;
        }

        // Cache light intensities
        foreach (var l in lights)
        {
            originalIntensities[l] = l.intensity;
        }
    }

    // Call this to update the state of the entire room (walls, lighting)
    public void SetState(RoomVisualState state)
    {
        if (wallSegments == null || wallSegments.Length == 0)
        {
            AssignWallSegmentsAndLights(); // Make sure we assign wall segments
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

    // Update the visibility (alpha) of walls and lights smoothly
    IEnumerator LerpVisuals(float multiplier)
    {
        if (this == null) yield break;
        if (!gameObject.activeInHierarchy) yield break;

        float time = 0f;

        // Cache initial light intensities and wall segment alphas
        Dictionary<Light, float> startLightValues = new();
        Dictionary<WallSegment, float> startAlphaValues = new();

        foreach (var l in lights)
        {
            if (l == null) continue;
            startLightValues[l] = l.intensity;
        }

        foreach (var segment in wallSegments)
        {
            if (segment == null) continue;
            startAlphaValues[segment] = originalWallAlphas[segment]; // Store initial alpha for each wall segment
        }

        // Smoothly interpolate the state change
        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = time / transitionDuration;

            // Lerp lights
            foreach (var l in lights)
            {
                if (l == null) continue;
                float target = originalIntensities[l] * multiplier;
                l.intensity = Mathf.Lerp(startLightValues[l], target, t);
            }

            yield return null;
        }
    }

    // Apply alpha value to the entire wall segment (all meshes in that segment)
    public void SetWallSegmentVisibility(WallSegment segment, float alpha)
    {
        MaterialPropertyBlock block = wallPropertyBlocks[segment];
        
        // Get the material of the renderer and ensure it supports transparency
        Renderer renderer = segment.GetComponentInChildren<Renderer>();
        
        // Get the current color (RGB) from the material
        Color currentColor = renderer.material.GetColor(BaseColorID);

        // Only modify the alpha channel, keep RGB the same
        Color newColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);

        // Apply the new color with updated alpha to the material property block
        block.SetColor(BaseColorID, newColor);  // Ensure we're using the correct color property

        // Apply the property block to all renderers in the wall segment
        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.SetPropertyBlock(block);
        }

        // Update the original alpha for the next transition
        originalWallAlphas[segment] = alpha;
    }

    // Method to get current color of a wall segment's renderer
    public Color GetWallSegmentColor(WallSegment segment)
    {
        MaterialPropertyBlock block = wallPropertyBlocks[segment];
        Renderer renderer = segment.GetComponentInChildren<Renderer>();
        renderer.GetPropertyBlock(block);
        Color color = block.GetColor(BaseColorID);
        // if the block didn't actually have a base color yet, fall back to the material's color
        if (Mathf.Approximately(color.a, 0f))
        {
            color = renderer.material.GetColor(BaseColorID);
            // also populate the block so subsequent calls return correctly
            block.SetColor(BaseColorID, color);
            renderer.SetPropertyBlock(block);
        }
        return color;
    }
}