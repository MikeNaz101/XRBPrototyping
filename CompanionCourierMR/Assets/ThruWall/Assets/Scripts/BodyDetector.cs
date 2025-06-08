using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyDetector : MonoBehaviour
{

    public Collider associatedSurface;
    public BodyDetector otherDetector;
    private Collider blockingMask;

    private Collider[] allColliders;
    private Rigidbody[] allRigidbodies;

    private Teleporter teleporter;

    // Use this for initialization
    void Start()
    {
        InitializeCollidersList();

        teleporter = transform.parent.GetComponentInChildren<Teleporter>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (associatedSurface == null || !otherDetector.gameObject.activeInHierarchy)
        {
            return;
        }
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider col = allColliders[i];
            Rigidbody rb = allRigidbodies[i];
            if (rb != null && col != blockingMask && col.transform != teleporter.transform)
            {
                float dist = Vector3.Distance(col.transform.position, transform.position);
                float otherDist = dist + 1.0f;
                if (otherDetector.associatedSurface == this.associatedSurface)
                {
                    otherDist = Vector3.Distance(col.transform.position, otherDetector.transform.position);
                }
                if (otherDist > dist)
                {
                    float threshold = Mathf.Max(1.0f, (2.0f / 3.5f) * (rb.linearVelocity.magnitude / GameConstants.SPEED_LIMIT));
                    if (dist < threshold)
                    {
                        Vector3 colPos = col.transform.position;
                        Vector3 colToMe = colPos - transform.position;
                        float ang = Vector3.Angle(colToMe, transform.forward);
                        float angleThreshold = col.tag == "Player" ? 90.0f : 120.0f;
                        if (ang < angleThreshold)
                        {
                            Physics.IgnoreCollision(associatedSurface, col, true);
                            float velAng = Vector3.Angle(rb.linearVelocity, transform.forward);
                            Physics.IgnoreCollision(col, blockingMask, false);
                        }
                        else
                        {
                            Physics.IgnoreCollision(col, blockingMask, true);
                            teleporter.TeleportCollider(col);
                            Physics.IgnoreCollision(associatedSurface, col, false);
                        }

                    }
                    else
                    {
                        Physics.IgnoreCollision(col, blockingMask, true);
                        Physics.IgnoreCollision(associatedSurface, col, false);
                    }
                }
            }
        }
    }

    public void InitializeCollidersList()
    {
        blockingMask = transform.parent.Find("BlockingMask").GetComponent<Collider>();
        allColliders = FindObjectsOfType<Collider>();
        allRigidbodies = new Rigidbody[allColliders.Length];
        int index = 0;
        foreach (Collider col in allColliders)
        {
            Physics.IgnoreCollision(col, blockingMask, true);
            allRigidbodies[index++] = col.GetComponent<Rigidbody>();
        }
    }
}
