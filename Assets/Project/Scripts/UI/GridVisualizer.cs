using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Dynamically generates and scales a grid overlay in the XZ plane to visualize spatial scale.
/// Features:
/// - Adjusts grid spacing based on camera distance (logarithmic scaling)
/// - Labels axes at intervals that adapt to zoom level
/// - Limits visible labels to prevent clutter (max 30 per axis)
/// - Formats numbers in scientific notation for large distances
/// </summary>
public class GridVisualizer : MonoBehaviour {
    [Header("Dependencies")]
    public CameraController cameraController;  // For tracking zoom level

    [Header("Runtime State")]
    [Tooltip("Current distance between grid labels in world units.")]
    public float currentSpacing = -1f;  // -1 = uninitialized

    [Tooltip("Precomputed 2*tan(60°) for projection calculations.")]
    private static float tan60_times2 = 2 * 1.732050807f;  // Used for frustum-based scaling

    /// <summary>
    /// Initializes the grid system and starts the dynamic scaling coroutine.
    /// </summary>
    void Start() {
        cameraController = FindFirstObjectByType<CameraController>();
        StartCoroutine(UpdateGridScale());  // Begin adaptive grid updates
    }


    /// <summary>
    /// Dynamically adjusts grid spacing based on camera distance:
    /// 1. Calculates base spacing from log10 of camera distance
    /// 2. Refines spacing to human-readable intervals (1, 2, or 5 x 10^n)
    /// 3. Triggers redraw if spacing changes
    /// Runs every second to balance performance and responsiveness.
    /// </summary>
    IEnumerator UpdateGridScale() {
        while (true) {
            
            float distance = cameraController.distance;
            distance *= tan60_times2;
            int exponent = Mathf.FloorToInt(Mathf.Log10(distance));
            float spacing = Mathf.Pow(10, exponent);
            int oneSigFig = Mathf.FloorToInt(distance / spacing);

            // The sigFig checks (2, 4, 8) create these spacing rules:
            // - If leading digit is 1:    Use 1x10^(n-1)
            // - If leading digit is 2-3:  Use 2x10^(n-1)
            // - If leading digit is 4-7:  Use 5x10^(n-1)
            // This ensures an appropriate distance between the markings depending on how zoomed in/out the user is.
            int sigFig = Mathf.FloorToInt(distance / spacing);
            if (sigFig < 2) {
                spacing /= 10;
            } 
            else if (sigFig < 4) {
                spacing /= 5f;
            } 
            else if (sigFig < 8) {
                spacing /= 2f;
            }

            // Redraw only if spacing changed
            if (currentSpacing != spacing) {
                currentSpacing = spacing;
                RedrawGrid(distance);
            }
            yield return new WaitForSeconds(1f);  // Throttle updates
        }
    }

    /// <summary>
    /// Clears existing labels and redraws the grid:
    /// - Labels are placed along X and Z axes
    /// - Limited to ±15 intervals or 2.5e7 units (whichever is smaller)
    /// - Destroys old labels before creating new ones
    /// </summary>
    void RedrawGrid(float distance) {
        // Clear previous labels
        foreach (Transform child in transform) {
            if (child.name.StartsWith("GridLabel")) Destroy(child.gameObject);
        }

        // Calculate visible bounds
        float extent = Mathf.Min(15f * currentSpacing, 2.5e7f);  // Clamp to prevent overflow

        // Draw X-axis labels
        for (float x = -extent; x <= extent; x += currentSpacing) {
            CreateLabel(new Vector3(x, 0, 0), FormatNumber(x), distance);
        }

        // Draw Z-axis labels
        for (float z = -extent; z <= extent; z += currentSpacing) {
            CreateLabel(new Vector3(0, 0, z), FormatNumber(z), distance);
        }
    }
    
    /// <summary>
    /// Formats numbers for grid labels:
    /// - Values < 1: Show as whole numbers ("0")
    /// - Values ≥ 1: Scientific notation with 2 decimals ("1.23e+4")
    /// </summary>
    string FormatNumber(float val) {
        if (Mathf.Abs(val) < 1f) return val.ToString("F0");  // Small numbers: "0"
        return val.ToString("0.##e+0\n|");  // Large numbers: scientific notation
    }

    /// <summary>
    /// Creates a TextMesh label at the specified position:
    /// - Rotates 90° to lie flat on the XZ plane
    /// - Scales text size with camera distance
    /// - Uses white text with centered alignment
    /// </summary>
    void CreateLabel(Vector3 pos, string text, float distance) {
        GameObject labelObj = new GameObject("GridLabel_" + text);
        labelObj.transform.position = pos;
        labelObj.transform.parent = transform;

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = distance / 700f;  // Dynamic scaling
        tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        labelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);  // Lay flat
    }

}
