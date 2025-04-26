using UnityEngine;

/// <summary>
/// Controls a camera with orbit, pan, and zoom functionality.
/// Features:
/// - Right-click + drag to orbit around a target point.
/// - Left-click + drag to pan the camera.
/// - Mouse wheel to zoom in/out with logarithmic sensitivity.
/// - Constrained pitch to prevent camera flipping.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour {
    [Header("Camera Settings")]
    [Tooltip("Initial distance from the target point (world units).")]
    public float distance = 8e5f;

    [Tooltip("The point the camera focuses on (world position).")]
    public Vector3 target = Vector3.zero;

    [Header("Control Speeds")]
    [Tooltip("Base speed for zooming (scaled logarithmically with distance).")]
    public float zoomSpeed = 2e3f;

    [Tooltip("Rotation speed when orbiting (right-click + drag).")]
    public float tiltSpeed = 0.1f;

    [Tooltip("Movement speed when panning (left-click + drag).")]
    public float panSpeed = 0.001f;

    [Header("Internal State")]
    [SerializeField, Tooltip("Horizontal rotation angle (degrees).")]
    private float yaw = 0f;

    [SerializeField, Tooltip("Vertical rotation angle (degrees). Clamped to [10, 170].")]
    private float pitch = 90f;  // Default: Looking straight down

    private Camera cam;
    private Vector3 lastMousePosition;  // Stores mouse position for delta calculations

    /// <summary>
    /// Initializes the camera component and sets clipping planes.
    /// </summary>
    void Start() {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 10;
        cam.farClipPlane = 5e7f;
        UpdateCameraPosition();
    }

    /// <summary>
    /// Processes input and updates the camera position every frame.
    /// </summary>
    void Update() {
        HandleInput();
        UpdateCameraPosition();
    }

    /// <summary>
    /// Handles zoom, orbit, and pan inputs.
    /// </summary>
    void HandleInput() {
        // Zoom with scroll wheel (along the camera's forward vector)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f) {
            // Logarithmic zoom sensitivity (faster at larger distances)
            float dynamicZoomSpeed = Mathf.Log10(distance) * Mathf.Log10(distance) * zoomSpeed;
            if (distance > 1e6f) dynamicZoomSpeed *= 5;  // Extra boost for far distances
            
            distance = Mathf.Clamp(
                distance - scroll * dynamicZoomSpeed,
                Constraints.MIN_CAMERA_DISTANCE,
                Constraints.MAX_CAMERA_DISTANCE
            );
        }

        // --- Orbit (Right-Click) ---
        if (Input.GetMouseButtonDown(1)) {
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(1)) {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            yaw += delta.x * tiltSpeed;              // Horizontal rotation
            pitch -= delta.y * tiltSpeed;             // Vertical rotation
            pitch = Mathf.Clamp(pitch, 10f, 170f);    // Prevent flipping
            lastMousePosition = Input.mousePosition;  // Update for next frame
        }

        // --- Pan (Left-Click) ---
        if (Input.GetMouseButtonDown(0)) {
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(0)) {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            // Calculate pan direction in world space
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            Vector3 panMovement = (-right * delta.x + -up * delta.y) * distance * panSpeed;
            target += panMovement;  // Move the focus point
            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// Updates the camera's position and rotation based on current yaw/pitch/distance.
    /// </summary>
    void UpdateCameraPosition() {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.forward;
        transform.position = target - direction * distance;  // Position camera behind the target
        transform.rotation = rotation;                      // Apply rotation
    }
    
}