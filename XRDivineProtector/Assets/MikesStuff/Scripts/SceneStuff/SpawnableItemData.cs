// SpawnableItemData.cs
using UnityEngine;

[System.Serializable]
public class SpawnableItemData
{
    public string itemName;
    public GameObject itemPrefab;
    public Sprite itemIcon;
    public bool isWall = false; // <-- ADD THIS LINE
}