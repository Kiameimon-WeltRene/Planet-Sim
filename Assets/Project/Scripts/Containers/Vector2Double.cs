// Vector3d.cs
using System;
using UnityEngine;

[Serializable]
public struct Vector2Double {
    public double x;
    public double z;

    public Vector2Double(double x, double z) {
        this.x = x;
        this.z = z;
    }

    // Conversion to Unity's Vector3
    readonly public Vector3 ToVector3() {
        return new Vector3((float)x, 0f, (float)z);
    }

    // Convert Vector3 to Vector2Double
    public void FromVector3(Vector3 v) {
        x = v.x;
        z = v.z;
    }

    // Vector operations
    public static Vector2Double operator +(Vector2Double a, Vector2Double b) {
        return new Vector2Double(a.x + b.x, a.z + b.z);
    }
    
    public static Vector2Double operator -(Vector2Double a, Vector2Double b) {
        return new Vector2Double(a.x - b.x, a.z - b.z);
    }

    public static Vector2Double operator *(double s, Vector2Double a) {
        return new Vector2Double(a.x * s, a.z * s);
    }

    public static Vector2Double operator *(Vector2Double a, double s) {
        return new Vector2Double(a.x * s, a.z * s);
    }
    
    public static Vector2Double operator /(Vector2Double a, double s) {
        return new Vector2Double(a.x / s, a.z / s);
    }

    readonly public double SqrMagnitude {
        get { return x * x + z * z; }
    }
    readonly public double Magnitude {
        get { return Mathf.Sqrt((float)SqrMagnitude); }
    }

    readonly public Vector2Double Normalized {
        get { return (1.0 / Magnitude) * this; }
    }
    public static Vector2Double one  {
        get { return new Vector2Double(1, 1);}
    }
    public static Vector2Double zero  {
        get { return new Vector2Double(0, 0);}
    }

}