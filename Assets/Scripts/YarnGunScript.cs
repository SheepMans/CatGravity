using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Simple grappling system using a DistanceJoint2D and LineRenderer.
/// Attach this script to the player. It shoots a ray toward the mouse,
/// attaches a DistanceJoint2D when hitting a valid surface, and draws a rope.
/// </summary>
[RequireComponent(typeof(DistanceJoint2D))]
public class YarnGunScript : MonoBehaviour
{
    [Header("Grapple Settings")]
    [SerializeField] private float grappleLength = 5f;       // Distance from player to grapple point
    [SerializeField] private LayerMask grappleLayer;         // Layers you can attach to

    [Header("Rope Visuals")]
    [SerializeField] private LineRenderer yarn;              // Rope line renderer

    [Header("References")]
    [SerializeField] public CatController catController;     // Reference to the CatController script

    [Header("Yarn Pictures")]
    [SerializeField] public Image yarn1;                     // UI element to display yarn count
    [SerializeField] public Image yarn2;                     // UI element to display yarn count
    [SerializeField] public Image yarn3;                     // UI element to display yarn count
    [SerializeField] public float colorChangeSpeed = 1f;      // Speed of color change for yarn UI

    [Header("Pull Settings")]
    public float pullSpeed = 5f;                           // Speed at which the rope pulls the player in

    private Vector3 grapplePoint;                            // Where the rope connects
    private DistanceJoint2D joint;                           // Reference to the DistanceJoint2D component

    private Camera cam;                                      // Cached camera reference

    private void Start()
    {
        // Cache references
        joint = GetComponent<DistanceJoint2D>();
        cam = Camera.main;

        // Disable by default
        joint.enabled = false;
        yarn.enabled = false;

        // Ensure LineRenderer uses world coordinates
        yarn.useWorldSpace = true;
        yarn.positionCount = 2;
    }

    private void Update()
    {
        Color visibleColor = new Color(1f, 1f, 1f, 1f);
        Color hiddenColor = new Color(0f, 0f, 0f, 1f);


        int yarnCount = catController.getYarnCount();
        // Lerp each yarn image color towards its target
        yarn1.color = Color.Lerp(yarn1.color, yarnCount >= 1 ? visibleColor : hiddenColor, colorChangeSpeed * Time.deltaTime);
        yarn2.color = Color.Lerp(yarn2.color, yarnCount >= 2 ? visibleColor : hiddenColor, colorChangeSpeed * Time.deltaTime);
        yarn3.color = Color.Lerp(yarn3.color, yarnCount >= 3 ? visibleColor : hiddenColor, colorChangeSpeed * Time.deltaTime);

        if (catController.getYarnCount() <= 0)
        {
            return; // No yarn left, do nothing
        }
        // ---------- SHOOT GRAPPLE ----------
        if (Input.GetMouseButtonDown(0))
        {
            // Convert mouse to world position
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorldPos - transform.position).normalized;

            // Cast a ray *in that direction*
            RaycastHit2D hit = Physics2D.Raycast(
                origin: transform.position,   // Start from player
                direction: direction,         // Toward mouse
                distance: Mathf.Infinity,
                layerMask: grappleLayer
            );

            // If we hit a valid object
            if (hit.collider != null)
            {
                grapplePoint = hit.point;
                joint.connectedAnchor = grapplePoint;

                // Set rope length (distance) to either the hit distance or your preset value
                joint.distance = grappleLength > 0 ? grappleLength : Vector2.Distance(transform.position, grapplePoint);
                joint.enabled = true;

                // Set LineRenderer positions
                yarn.SetPosition(0, grapplePoint);
                yarn.SetPosition(1, transform.position);
                yarn.enabled = true;


            }
        }


        


        // ---------- UPDATE ROPE EACH FRAME ----------
        if (yarn.enabled)
        {
            yarn.SetPosition(0, grapplePoint);      // Anchor
            yarn.SetPosition(1, transform.position); // Player end



            // ---------- RELEASE GRAPPLE ----------
            float currentDistance = Vector2.Distance(transform.position, grapplePoint);
            if (joint.enabled && currentDistance <= joint.distance + 0.05f) // small buffer for precision
            {
                joint.enabled = false;
                yarn.enabled = false;
                catController.useYarn();
                Debug.Log("Yarn auto-disconnected! Current yarn: " + catController.getYarnCount());
            }

            if (Input.GetMouseButtonUp(0))
            {
                joint.enabled = false;
                yarn.enabled = false;
                catController.useYarn();
                Debug.Log("Yarn used! Current yarn: " + catController.getYarnCount());

            }
        }
    }

}
