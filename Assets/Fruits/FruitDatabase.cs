using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Suika/Fruit Database")]
public class FruitDatabase : ScriptableObject
{
    [Serializable]
    public struct FruitInfo
    {
        public string name;
        public Fruit prefab;
        public Sprite icon;     // optional override
        public int basePoints;  // points for creating this level via merge
    }

    [SerializeField] private List<FruitInfo> fruits = new();

    public int Count => fruits.Count;
    public int MaxLevel => fruits.Count - 1;

    public bool IsValidLevel(int level) => level >= 0 && level < fruits.Count;
    public bool HasNext(int level) => IsValidLevel(level) && level + 1 < fruits.Count;

    public Fruit GetPrefab(int level)
    {
        if (!IsValidLevel(level)) return null;
        return fruits[level].prefab;
    }

    public int GetBasePoints(int level)
    {
        if (!IsValidLevel(level)) return 0;
        return fruits[level].basePoints;
    }

    public Sprite GetIcon(int level)
    {
        if (!IsValidLevel(level)) return null;

        var info = fruits[level];
        if (info.icon) return info.icon;

        if (info.prefab)
        {
            var sr = info.prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr) return sr.sprite;
        }
        return null;
    }
}
