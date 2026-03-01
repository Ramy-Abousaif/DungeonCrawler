using System.Collections.Generic;
using UnityEngine;

public class WallSegment : MonoBehaviour
{
    // This will hold all the meshes forming the wall segment
    public List<Renderer> wallMeshes;

    // Method to set visibility for the whole segment
    public void SetVisibility(float alpha)
    {
        foreach (var mesh in wallMeshes)
        {
            var propertyBlock = new MaterialPropertyBlock();
            mesh.GetPropertyBlock(propertyBlock);
            Color color = propertyBlock.GetColor("_BaseColor");
            color.a = alpha;
            propertyBlock.SetColor("_BaseColor", color);
            mesh.SetPropertyBlock(propertyBlock);
        }
    }

    // Add a mesh renderer to the segment
    public void AddMesh(Renderer renderer)
    {
        if (wallMeshes == null)
            wallMeshes = new List<Renderer>();
        
        wallMeshes.Add(renderer);
    }
}