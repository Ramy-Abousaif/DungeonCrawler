using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dungeon/Room Prefab Database")]
public class RoomPrefabDatabase : ScriptableObject
{
    [System.Serializable]
    public class RoomPrefabEntry
    {
        public RoomType type;
        public GameObject prefab;
        public Material mat;

        [Header("Layout")]
        public Vector2Int gridSize = Vector2Int.one;
        public int height = 1;
    }

    public List<RoomPrefabEntry> entries;

    public RoomPrefabEntry GetEntry(RoomType type)
    {
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry;
        }

        Debug.LogWarning("Missing prefab for room type: " + type);
        return null;
    }
}