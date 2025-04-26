using UnityEngine;

/// <summary>
/// Tracks the number of full orbits completed by a celestial body (e.g., a planet).
/// Logs the average time taken per orbit when the target count is reached.
/// Note: Assumes the body is orbiting counterclockwise in the XZ plane.
/// </summary>
public class OrbitTimer : MonoBehaviour {
    [Tooltip("Current angle (degrees) relative to the origin in the XZ plane.")]
    public float curAngle = 0f;

    [Tooltip("Target number of orbits to complete before logging results.")]
    public static int orbits = 1;

    private bool completed = false;  // Flag to stop tracking after completion
    private float startTime;        // Timestamp when tracking began
    private int counter = 0;        // Counts partial angle resets (360° -> 0°)

    /// <summary>
    /// Initializes the timer when the object is created.
    /// </summary>
    void Start() {
        startTime = Time.time;
    }

    /// <summary>
    /// Checks for completed orbits by monitoring angular position changes.
    /// Logs the average orbit duration when the target count is reached.
    /// </summary>
    void Update() {
        if (completed) return;
        // Calculate current angle in degrees (0-360) from XZ coordinates
        float newAngle = Mathf.Atan2(transform.position.z, transform.position.x) * Mathf.Rad2Deg;
        
        if (newAngle < 0) newAngle += 360f; // Convert (-180° to 180°) to (0° to 360°)
        
        // Detect when the angle wraps around (360° -> 0° = 1 partial orbit)
        if (newAngle < curAngle) {
            counter++;
        }

        // Check if target orbit count is reached
        if (counter == orbits) {
            Debug.Log($"{gameObject.name} completed {orbits} full orbit(s) in {1e7f * (Time.time-startTime)/orbits} seconds on average.");
            completed = true;  // Stop further checks
        }

        curAngle = newAngle;  // Update the angle for the next frame
    }
    
}
