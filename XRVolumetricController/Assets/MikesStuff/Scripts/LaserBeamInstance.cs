// LaserBeamInstance.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For HashSet
using System.Linq; // For OrderBy

[RequireComponent(typeof(LineRenderer))]
public class LaserBeamInstance : MonoBehaviour
{
    [Header("Laser Properties")]
    public float maxDistance = 100f;
    public float damagePerSecond = 30f;
    public LayerMask hitLayerMask;         // Set in Inspector on the Prefab
    public float beamWidth = 0.1f;
    public GameObject impactEffectPrefab;   // Optional particle effect for hits
    public float lifetime = 1f;           // Default lifetime for this beam instance

    [Header("Debugging")]
    [Tooltip("Enable verbose logging in Update. Can be spammy.")]
    public bool enableVerboseUpdateLogging = false;
    [Tooltip("Logs initial setup values once in Start.")]
    public bool enableStartupLogging = true;

    // Public fields for inspecting values at runtime
    [Space(10)]
    [Header("Runtime Debug Info (Read-Only)")]
    public Vector3 Dbg_BeamGlobalOrigin;
    public Vector3 Dbg_BeamGlobalDirection;
    public Vector3 Dbg_VisualEndPoint;
    public bool Dbg_LineRendererEnabled;
    public int Dbg_LineRendererPositionCount;
    public Material Dbg_LineRendererMaterial;
    public float Dbg_CurrentLifetimeRemaining;


    private LineRenderer lineRenderer;
    private Vector3 beamGlobalOriginInternal;   // Renamed to avoid confusion with public debug var
    private Vector3 beamGlobalDirectionInternal; // Renamed

    public void Initialize(float specificLifetime, Vector3 origin, Vector3 direction)
    {
        this.lifetime = specificLifetime;
        this.beamGlobalOriginInternal = origin;
        this.beamGlobalDirectionInternal = direction.normalized;

        if (enableStartupLogging)
        {
            Debug.Log($"[{gameObject.name}] Initialized with Lifetime: {specificLifetime}, Origin: {origin}, Direction: {direction.normalized}");
        }
        Dbg_BeamGlobalOrigin = beamGlobalOriginInternal;
        Dbg_BeamGlobalDirection = beamGlobalDirectionInternal;
    }

    void Start()
    {
        Dbg_CurrentLifetimeRemaining = lifetime;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] LaserBeamInstance prefab is missing a LineRenderer component! Destroying self.");
            Destroy(gameObject);
            return;
        }
        Dbg_LineRendererEnabled = lineRenderer.enabled; // Initial state

        // Check if Initialize was called (beamGlobalDirectionInternal would be non-zero)
        // If not, use the prefab's transform at spawn time as a fallback.
        if (beamGlobalDirectionInternal == Vector3.zero)
        {
            if (enableStartupLogging) Debug.LogWarning($"[{gameObject.name}] Initialize() was not called before Start(). Using transform.position and transform.forward as fallback for beam origin/direction.");
            beamGlobalOriginInternal = transform.position;
            beamGlobalDirectionInternal = transform.forward;
            Dbg_BeamGlobalOrigin = beamGlobalOriginInternal; // Update debug var
            Dbg_BeamGlobalDirection = beamGlobalDirectionInternal; // Update debug var
        }
        
        if (lineRenderer.material == null)
        {
            Debug.LogError($"[{gameObject.name}] LineRenderer has NO MATERIAL assigned! It will not be visible. Please assign a material to the LineRenderer component on the prefab.", this);
            lineRenderer.enabled = false; // Disable it if no material
            Dbg_LineRendererEnabled = false;
            Destroy(gameObject, 0.1f); // Destroy shortly as it's misconfigured
            return;
        }
        Dbg_LineRendererMaterial = lineRenderer.material; // Store for debugging

        lineRenderer.enabled = true;
        Dbg_LineRendererEnabled = lineRenderer.enabled;

        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;

        if (beamWidth <= 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Beam width is {beamWidth}. Line may be invisible or very thin.", this);
        }

        // Set initial positions for the first frame.
        lineRenderer.SetPosition(0, beamGlobalOriginInternal);
        lineRenderer.SetPosition(1, beamGlobalOriginInternal + beamGlobalDirectionInternal * maxDistance); // Default end before first raycast
        Dbg_LineRendererPositionCount = lineRenderer.positionCount;

        if (enableStartupLogging)
        {
            Debug.Log($"[{gameObject.name}] Started. Material: {lineRenderer.material.name}, Width: {beamWidth}, Initial Origin: {lineRenderer.GetPosition(0)}, Initial Default End: {lineRenderer.GetPosition(1)}");
            if (lineRenderer.useWorldSpace)
            {
                Debug.Log($"[{gameObject.name}] LineRenderer is using World Space.");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] LineRenderer is using Local Space. For a beam fired from a moving object that should appear fixed in world space after firing (as this script implies), 'Use World Space' should be TRUE on the LineRenderer.", this);
            }
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        Dbg_CurrentLifetimeRemaining -= Time.deltaTime;
        if (lineRenderer == null || !lineRenderer.enabled)
        {
            Dbg_LineRendererEnabled = lineRenderer != null ? lineRenderer.enabled : false;
            return;
        }
        Dbg_LineRendererEnabled = lineRenderer.enabled; // Keep this updated

        // The beam's origin and direction are fixed after firing for this instance
        // Ensure the LineRenderer positions are updated if it's in world space
        // and its parent transform might have moved (though for a fired beam, origin should be static).
        lineRenderer.SetPosition(0, beamGlobalOriginInternal);
        Dbg_BeamGlobalOrigin = beamGlobalOriginInternal; // Already set but good to be sure

        // --- Perform Raycasts ---
        RaycastHit primaryVisualHit;
        Vector3 visualEndPointActual; // Renamed for clarity
        bool hitSomething = Physics.Raycast(beamGlobalOriginInternal, beamGlobalDirectionInternal, out primaryVisualHit, maxDistance, hitLayerMask);

        if (hitSomething)
        {
            visualEndPointActual = primaryVisualHit.point;
        }
        else
        {
            visualEndPointActual = beamGlobalOriginInternal + (beamGlobalDirectionInternal * maxDistance);
        }
        lineRenderer.SetPosition(1, visualEndPointActual);
        Dbg_VisualEndPoint = visualEndPointActual; // Update debug var
        Dbg_LineRendererPositionCount = lineRenderer.positionCount;


        if (enableVerboseUpdateLogging && Time.frameCount % 30 == 0) // Log every 30 frames to reduce spam
        {
            Debug.Log($"[{gameObject.name}] Update. Origin: {beamGlobalOriginInternal}, End: {visualEndPointActual}, Hit: {hitSomething}");
        }


        // --- Collision & Damage ---
        // Only apply damage if the beam actually hit something visually or is intended to hit up to its visual end point
        float actualBeamLength = Vector3.Distance(beamGlobalOriginInternal, visualEndPointActual);
        RaycastHit[] allHits = Physics.RaycastAll(beamGlobalOriginInternal, beamGlobalDirectionInternal, actualBeamLength, hitLayerMask);

        HashSet<GameObject> hitObjectsThisFrame = new HashSet<GameObject>();

        foreach (RaycastHit hit in allHits.OrderBy(h => h.distance))
        {
            GameObject rootHitObject = hit.collider.gameObject; // Using rootHitObject to avoid multiple damage calls on compound colliders if not desired
            if (hit.collider.transform.root != null) // Prefer root if available
            {
                rootHitObject = hit.collider.transform.root.gameObject;
            }


            if (hitObjectsThisFrame.Contains(rootHitObject))
            {
                continue; // Already processed this root object this frame
            }
            hitObjectsThisFrame.Add(rootHitObject);

            // Try to get IDamageable from the actual hit collider's GameObject first, then its Rigidbody, then its root.
            IDamageable damageableObject = hit.collider.GetComponent<IDamageable>();
            if (damageableObject == null && hit.rigidbody != null)
            {
                damageableObject = hit.rigidbody.GetComponent<IDamageable>();
            }
            if (damageableObject == null && hit.collider.transform.root != null)
            {
                 damageableObject = hit.collider.transform.root.GetComponent<IDamageable>();
            }


            if (damageableObject != null)
            {
                if(enableVerboseUpdateLogging && Time.frameCount % 30 == 0) Debug.Log($"[{gameObject.name}] Damaging: {rootHitObject.name}");
                damageableObject.TakeDamage(damagePerSecond * Time.deltaTime);
            }
            else
            {
                 if(enableVerboseUpdateLogging && Time.frameCount % 30 == 0 && hitSomething) Debug.Log($"[{gameObject.name}] Hit {rootHitObject.name} but it has no IDamageable component.");
            }


            if (impactEffectPrefab != null && hitSomething) // Only spawn impact if primary raycast also hit
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }
}
