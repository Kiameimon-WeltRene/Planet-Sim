using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core gravitational simulation manager that:
/// - Maintains a list of celestial bodies (planets)
/// - Calculates N-body physics using leapfrog integration
/// - Handles time scaling and visual scaling adjustments
/// - Provides planet creation/management APIs
/// 
/// Units:
/// - Distances: Earth radii (6,371 km per unit)
/// - Masses: Earth masses (5.972 × 10^24 kg per unit)
/// - Time: Custom scaled seconds (timeScale modifies real time)
/// </summary>
public class Simulator : MonoBehaviour {
    [Header("Singleton Instance")]
    public static Simulator Instance;  // Global access point

    [Header("Dependencies")]
    public CameraController cameraController;  // For distance-based scaling

    [Header("Physics Constants")]
    private static double gravity_constant = 1.53758e-6f;  // Scaled gravitational constant
    public static int iterationsPerUpdate = 4;  // Physics steps per FixedUpdate

    [Header("Simulation Control")]
    [Tooltip("Time multiplier for simulation speed (1e7 = 10 million x real time)")]
    public float timeScale = 1e7f;
    private float changeInTime;  // Scaled timestep per iteration

    [Header("Planet Management")]
    public GameObject planetPrefab;  // Template for instantiating planets
    public List<Planet> planets = new();  // Active celestial bodies

    [Header("Visual Scaling")]
    private float relativeMinRadiusLog;  // Log of smallest planet radius
    private float relativeMaxRadusLog;   // Log of largest planet radius

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initializes the solar system with default planets.
    /// Positions are scaled by 23454 (AU to Earth radii conversion).
    /// Velocities are tuned for stable orbits.
    /// </summary>
    void Start() {
        cameraController = FindFirstObjectByType<CameraController>();

        changeInTime = Time.fixedDeltaTime * timeScale / iterationsPerUpdate;
        // Planetary data (mass/radius relative to Earth)
        AddPlanet("Sun", Vector3.zero, Vector3.zero, 332946f, 109.12336f, new Color(1f, 1f, 0f));
        AddPlanet("Mercury", new Vector3(0.387f * 23454f, 0, 0), new Vector3(0, 0, 0.00738f), 0.0553f, 0.38297f, Color.gray);
        AddPlanet("Venus", new Vector3(0.723f * 23454f, 0, 0), new Vector3(0, 0, 0.00549f), 0.815f, 0.94885f, new Color(1f, 0.7f, 0.3f));
        AddPlanet("Earth", new Vector3(1.0f * 23454f, 0, 0), new Vector3(0, 0, 0.00471f), 1f, 1f, new Color(107f/255f, 147f/255f, 214f/255f));
        AddPlanet("Mars", new Vector3(1.524f * 23454f, 0, 0), new Vector3(0, 0, 0.00377f), 0.107f, 0.53200f, new Color(1f, 0.3f, 0f));
        AddPlanet("Jupiter", new Vector3(5.203f * 23454f, 0, 0), new Vector3(0, 0, 0.00204f), 317.8f, 10.97330f, new Color(1f, 0.6f, 0.2f));
        AddPlanet("Saturn", new Vector3(9.537f * 23454f, 0, 0), new Vector3(0, 0, 0.00152f), 95.2f, 9.14026f, new Color(0.9f, 0.8f, 0.6f));
        AddPlanet("Uranus", new Vector3(19.191f * 23454f, 0, 0), new Vector3(0, 0, 0.00107f), 14.5f, 3.98097f, new Color(0.5f, 0.9f, 1f));
        AddPlanet("Neptune", new Vector3(30.07f * 23454f, 0, 0), new Vector3(0, 0, 0.00085f), 17.1f, 3.86471f, Color.blue);
        AddPlanet("Pluto", new Vector3(39.48f * 23454f, 0, 0), new Vector3(0, 0, 0.00074f), 0.0022f, 0.18688f, new Color(0.8f, 0.8f, 0.8f));

        foreach (Planet planet in planets) {
            relativeMinRadiusLog = Mathf.Min(relativeMinRadiusLog, planet.logRadius);
            relativeMaxRadusLog = Mathf.Max(relativeMaxRadusLog, planet.logRadius);
        }
    }
    
    /// <summary>
    /// Update the planets' visual appearance every frame.
    /// This is to prevent planets from being too small to be seen on the simulator,
    /// as the distance between planets in planetary systems are typically larger than
    /// the planets' radii by many orders of magnitude.
    /// </summary>
    void Update() {
        RescalePlanetRadii(cameraController.distance);
    }

    void FixedUpdate() {
        for (int _ = 0; _ < iterationsPerUpdate; _++) {
            CalculatePlanetsKinematics(changeInTime);
        }

        for (int i = 0; i < planets.Count; i++) {
            planets[i].UpdateVisualPosition();
        }
    }

    /// <summary>
    /// Instantiates a new planet with given parameters.
    /// </summary>
    /// <param name="mass">Earth masses (1 = 5.97e24 kg)</param>
    /// <param name="radius">Earth radii (1 = 6,371 km)</param>
    public void AddPlanet(string name, Vector3 pos, Vector3 vel, float mass, float radius, Color color) {
        GameObject obj = Instantiate(planetPrefab, pos, Quaternion.identity);
        obj.name = $"{name} ({mass} M⊕)"; // M⊕ = Earth mass symbol
        
        Planet p = obj.GetComponent<Planet>();
        p.position.FromVector3(pos);
        p.velocity.FromVector3(vel);
        p.mass = mass;
        p.radius = radius;
        p.logRadius = Mathf.Log(radius);
        p.color = color;
        planets.Add(p);
    }

    // ====================================================
    // =================== Physics core ===================
    // ====================================================

    /// <summary>
    /// Calculates gravitational acceleration on planet[i] from all other bodies.
    /// Uses Newtonian gravity: F = G * (m1*m2)/r^2.
    /// </summary>
    Vector2Double CalculateAcceleration(int i) {
        Vector2Double accel = new(0, 0);
        for (int j = 0; j < planets.Count; j++) {
            if (j == i) continue;
            
            Vector2Double displacement = planets[j].position - planets[i].position;
            accel += gravity_constant * planets[j].mass / displacement.SqrMagnitude * displacement.Normalized;
        }
        return accel;
    }

    /// <summary>
    /// Leapfrog integration (velocity Verlet variant):
    /// 1. Update positions using current velocity and acceleration
    /// 2. Calculate new accelerations
    /// 3. Update velocities using averaged old/new accelerations
    /// 
    /// More stable than Euler for orbital mechanics.
    /// </summary>
    void CalculatePlanetsKinematics(double dt) {
        // Position update (x += v*dt + 0.5*a*dt²)
        for (int i = 0; i < planets.Count; i++) {
            planets[i].position += planets[i].velocity * dt + 0.5 * planets[i].oldAcceleration * dt * dt;
        }

        // Velocity update (v += 0.5*(a_old + a_new)*dt)
        for (int i = 0; i < planets.Count; i++) {
            planets[i].oldAcceleration = planets[i].acceleration;
            planets[i].acceleration = CalculateAcceleration(i);
            planets[i].velocity += 0.5 * (planets[i].oldAcceleration + planets[i].acceleration) * dt;
        }
    }



    // ===================================================
    // ============== Visual scaling system ==============
    // ===================================================

    
    /// <summary>
    /// Base scaling function f(d):
    /// Maps camera distance [MIN_CAMERA_DISTANCE, MAX_CAMERA_DISTANCE] 
    /// to visual radii [1e3, 1e5] via linear interpolation.
    /// Defines the size of a reference planet (r = 0.1 Earth radii).
    /// </summary>
    float f(float d) {
        return Mathf.Lerp(1e3f, 1e5f, (d - Constraints.MIN_CAMERA_DISTANCE) /
        (Constraints.MAX_CAMERA_DISTANCE - Constraints.MIN_CAMERA_DISTANCE));
    }

    /// <summary>
    /// Relative scaling function g(d, logr):
    /// - First maps camera distance to a ratio range [MIN_RADIUS_RATIO, MAX_RADIUS_RATIO]
    /// - Then interpolates based on the planet's log-radius within the system's min/max
    /// Ensures small planets remain visible at large distances.
    /// </summary>
    float g(float d, float logr) {
        float x = Mathf.Lerp(Constraints.MIN_RADIUS_RATIO, Constraints.MAX_RADIUS_RATIO, 
        (d - Constraints.MIN_CAMERA_DISTANCE)/(Constraints.MAX_CAMERA_DISTANCE - Constraints.MIN_CAMERA_DISTANCE));
        float val = Mathf.Lerp(1f, x, (logr - relativeMinRadiusLog) / 
        (relativeMaxRadusLog - relativeMinRadiusLog));
        return val;
    }

    /// <summary>
    /// Applies visual scaling: 
    /// finalScale = f(distance) * g(distance, planet.logRadius)
    /// Where:
    /// - f(distance) = base size for the current zoom level
    /// - g() = multiplier based on the planet's relative size
    /// </summary>
    void RescalePlanetRadii(float distance) {
        float baseScale = f(distance);
        foreach (Planet planet in planets) {
            planet.UpdateVisualRadius(baseScale * g(distance, planet.logRadius));
        }
    }

}