using UnityEngine;

/// <summary>
/// Represents a celestial body (planet) in a physics simulation.
/// Handles position, velocity, acceleration, and rendering.
/// Units:
/// - Position: Earth radii (1 unit = 6,371 km)
/// - Velocity: Earth radii/second
/// - Mass: Earth masses
/// - Radius: Earth radii
/// </summary>
public class Planet : MonoBehaviour {
    // Physics State
    public Vector2Double position;   // Current position in 2D space (Earth radii)
    public Vector2Double velocity;   // Current velocity (Earth radii/second)
    public Vector2Double acceleration; // Current acceleration (calculated per frame)
    public Vector2Double oldAcceleration; // Previous frame's acceleration (for symplectic integration)
    public float mass;               // Mass relative to Earth (1 = Earth mass)

    // Rendering/Visuals
    public float radius;             // Physical size (Earth radii)
    public float logRadius;          // Logarithmic scale for visual adjustments (if used)
    public Color color;              // Display color of the planet

    /// <summary>
    /// Initializes the planet's collider, position, color, and physics state.
    /// </summary>
    void Start() {
        position.FromVector3(transform.position); // Sync Unity position with simulation
        GetComponent<Renderer>().material.color = color;
        acceleration = new Vector2Double(0, 0);   // Initialize physics
        oldAcceleration = new Vector2Double(0, 0);
    }

    /// <summary>
    /// Updates the planet's visual scale based on a multiplier.
    /// </summary>
    /// <param name="scale">Multiplier for the planet's radius.</param>
    public void UpdateVisualRadius(float scale) {
        transform.localScale = scale * Vector3.one;
    }

    /// <summary>
    /// Syncs the Unity GameObject's position with the simulation's 2D position.
    /// Call this after updating `position` in the physics simulation.
    /// </summary>
    public void UpdateVisualPosition() {
        transform.position = position.ToVector3();
    }

}
