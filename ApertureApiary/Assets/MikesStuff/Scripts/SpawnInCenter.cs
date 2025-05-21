using Meta.XR.MRUtilityKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnInCenter : MonoBehaviour
{
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] MRUKAnchor.SceneLabels label;
    [SerializeField] bool spawnOnbase;

    public void TriggerSpawn()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        foreach (var item in room.Anchors)
        {
            if (item.Label == label)
            {
                var obj = Instantiate(objectToSpawn, item.transform.position, Quaternion.identity);
                if (spawnOnbase)
                {
                    var anchorSize = item.VolumeBounds;
                    Debug.Log("Anchor size: " + anchorSize);
                    obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y - anchorSize.Value.extents.z * 2, obj.transform.position.z);
                }
                obj.transform.SetParent(transform);
            }
        }
    }
}