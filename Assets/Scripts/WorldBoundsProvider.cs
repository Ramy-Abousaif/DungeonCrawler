using UnityEngine;
using System.Collections.Generic;

public static class WorldBoundsProvider
{
    public static bool TryGetBounds(out Bounds bounds)
    {
        var box = Object.FindAnyObjectByType<WorldBoundsMarker>();
        if (box != null)
        {
            bounds = box.GetBounds();
            return true;
        }

        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Vector3 size = t.terrainData.size;
            Vector3 pos = t.transform.position;
            bounds = new Bounds(pos + size * 0.5f, size);
            return true;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        bounds = default;
        return false;
    }
}
