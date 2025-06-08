// SpawnableItemData.cs
using UnityEngine;

[System.Serializable] // Makes it show up in the Inspector if used in a List/Array
public class SpawnableItemData
{
    public string itemName;
    public GameObject itemPrefab;
    public Sprite itemIcon; // Optional, for UI buttons
    // Add other properties like cost, description, etc. if needed
}