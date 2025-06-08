using UnityEngine;

/// <summary>
/// Simple component to store grid data on an OVRSpatialAnchor.
/// It needs to be IOVRSyncable for Save/Load to automatically handle it,
/// or you handle its serialization manually. For this example, we read/write manually during Save/Load.
/// If using Meta's Cloud Anchors and Sharable Persistence, IOVRSyncable becomes more important.
/// </summary>
public class GridDataComponent : MonoBehaviour //, IOVRSyncable // Implement IOVRSyncable for more advanced persistence
{
    public float CellSize { get; private set; } = 0.5f;
    public System.Guid AssociatedFloorAnchorUuid { get; private set; } = System.Guid.Empty;

    public bool IsDataInitialized { get; private set; } = false;

    public void Initialize(float cellSize, System.Guid floorUuid)
    {
        CellSize = cellSize;
        AssociatedFloorAnchorUuid = floorUuid;
        IsDataInitialized = true;
        // Debug.Log($"GridDataComponent Initialized: CellSize={CellSize}, FloorUUID={AssociatedFloorAnchorUuid}");
    }

    // If implementing IOVRSyncable, you would add Read/Write methods here.
    // public void Read(OVRBinaryInputStream dataStream) { /* ... */ }
    // public void Write(OVRBinaryOutputStream dataStream) { /* ... */ }
    // public bool ShouldSync() { return true; }
}