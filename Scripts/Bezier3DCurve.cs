using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Immutable Bezier curve between two points </summary>
[System.Serializable]
public class Bezier3DCurve {

    /// <summary> Start point </summary>
    public Vector3 a { get { return _a; } }

    [SerializeField] private Vector3 _a;
    /// <summary> First handle. Local to start point </summary>
    public Vector3 b { get { return _b; } }

    [SerializeField] private Vector3 _b;
    /// <summary> Second handle. Local to end point </summary>
    public Vector3 c { get { return _c; } }

    [SerializeField] private Vector3 _c;
    /// <summary> End point </summary>
    public Vector3 d { get { return _d; } }

    [SerializeField] private Vector3 _d;

    /// <summary> B and C in world coordinates </summary>
    [SerializeField] private Vector3 _B, _C;
    /// <summary> Total length of the curve </summary>
    public float length { get { return _length; } }

    [SerializeField] private float _length;
    /// <summary> True if the curve is defined as a straight line </summary>
    public bool isLinear { get { return _isLinear; } }

    [SerializeField] private bool _isLinear;

    public AnimationCurve cache { get { return _cache; } }

    [SerializeField] private AnimationCurve _cache;
    [SerializeField] private Bezier3D.Vector3AnimationCurve _tangentCache;

    /// <summary> Constructor </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">First handle. Local to start point</param>
    /// <param name="c">Second handle. Local to end point</param>
    /// <param name="d">End point</param>
    public Bezier3DCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps) {
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _B = a + b;
        _C = d + c;
        _isLinear = b.sqrMagnitude == 0f && c.sqrMagnitude == 0f;
        _cache = GetDistanceCache(a, a + b, c + d, d, steps);
        _tangentCache = GetTangentCache(a, a + b, c + d, d, steps);
        _length = _cache.keys[_cache.keys.Length - 1].time;
    }

#region Public methods
    public void GetPoint(float t, out Vector3 result) {
        GetPoint(ref _a, ref _B, ref _C, ref _d, t, out result);
    }

    public Vector3 GetForward(float t) {
        return GetForward(_a, _B, _C, _d, t);
    }

    public Vector3 GetForwardFast(float t) {
        return _tangentCache.Evaluate(t);
    }

    public float Dist2Time(float distance) {
        return _cache.Evaluate(distance);
    }
#endregion

#region Private methods
    private static Bezier3D.Vector3AnimationCurve GetTangentCache(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps) {
        Bezier3D.Vector3AnimationCurve curve = new Bezier3D.Vector3AnimationCurve(); //time = distance, value = time
        float delta = 1f / steps;
        for (int i = 0; i < steps + 1; i++) {
            curve.AddKey(delta * i, GetForward(p0, p1, p2, p3, delta * i).normalized);
        }
        return curve;
    }

    private static AnimationCurve GetDistanceCache(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps) {
        AnimationCurve curve = new AnimationCurve(); //time = distance, value = time
        Vector3 prevPos = Vector3.zero;
        Vector3 newPos;
        float totalLength = 0f;
        for (int i = 0; i <= steps; i++) {
            //Normalize i
            float t = (float) i / (float) steps;
            //Get position from t
            GetPoint(ref p0, ref p1, ref p2, ref p3, t, out newPos);
            //First step
            if (i == 0) {
                //Add point at (0,0)
                GetPoint(ref p0, ref p1, ref p2, ref p3, 0, out prevPos);
                curve.AddKey(0, 0);
            }
            //Per step
            else {
                //Get distance from previous point
                float segmentLength = Vector3.Distance(prevPos, newPos);
                //Accumulate total distance traveled
                totalLength += segmentLength;
                //Save current position for next iteration
                prevPos = newPos;
                //Cache data
                curve.AddKey(totalLength, t);
            }
        }
        return curve;
    }

    public static void GetPoint(ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d, float t, out Vector3 result) {
        float u = 1f - t;
        float t2 = t * t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t3 = t2 * t;
        float p0scale = u3;
        float p1scale = (3f * u2 * t);
        float p2scale = (3f * u * t2);
        float p3scale = t3;

        result.x = p0scale * a.x + p1scale * b.x + p2scale * c.x + p3scale * d.x;
        result.y = p0scale * a.y + p1scale * b.y + p2scale * c.y + p3scale * d.y; // replace x with y
        result.z = p0scale * a.z + p1scale * b.z + p2scale * c.z + p3scale * d.z; // replace x with z
    }
    private static Vector3 GetForward(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) { //Also known as first derivative
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
        3f * oneMinusT * oneMinusT * (b - a) +
            6f * oneMinusT * t * (c - b) +
            3f * t * t * (d - c);
    }
#endregion
}