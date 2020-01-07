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
    public Vector3 GetPoint(float t) {
        return GetPoint(_a, _B, _C, _d, t);
    }

    public void GetPoint(float t, out Vector3 point) {
        GetPoint(ref _a, ref _B, ref _C, ref _d, t, out point);
    }

    public void GetForward(float t, out Vector3 forward) {
        GetForward(ref _a, ref _B, ref _C, ref _d, t, out forward);
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
        float totalLength = 0f;
        for (int i = 0; i <= steps; i++) {
            //Normalize i
            float t = (float) i / (float) steps;
            //Get position from t
            Vector3 newPos = GetPoint(p0, p1, p2, p3, t);
            //First step
            if (i == 0) {
                //Add point at (0,0)
                prevPos = GetPoint(p0, p1, p2, p3, 0);
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

    public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
        oneMinusT * oneMinusT * oneMinusT * a +
            3f * oneMinusT * oneMinusT * t * b +
            3f * oneMinusT * t * t * c +
            t * t * t * d;
    }

    private static Vector3 GetForward(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) { //Also known as first derivative
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
        3f * oneMinusT * oneMinusT * (b - a) +
            6f * oneMinusT * t * (c - b) +
            3f * t * t * (d - c);
    }

    private static void GetForward(ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d, float t, out Vector3 result) { //Also known as first derivative
        float oneMinusT = 1f - t;
        float baScale = 3f * oneMinusT * oneMinusT;
        float cbScale = 6f * oneMinusT * t;
        float dcScale = 3f * t * t;

        result.x = baScale * (b.x - a.x) + cbScale * (c.x - b.x) + dcScale * (d.x - c.x);
        result.y = baScale * (b.y - a.y) + cbScale * (c.y - b.y) + dcScale * (d.y - c.y);
        result.z = baScale * (b.z - a.z) + cbScale * (c.z - b.z) + dcScale * (d.z - c.z);
    }

    private static void GetPoint(ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d, float t, out Vector3 result) {
        float oneMinusT = 1f - t;
        float aScale = oneMinusT * oneMinusT * oneMinusT;
        float bScale = 3f * oneMinusT * oneMinusT * t;
        float cScale = 3f * oneMinusT * t * t;
        float dScale = t * t * t;

        result.x = aScale * a.x + bScale * b.x + cScale * c.x + dScale * d.x;
        result.y = aScale * a.y + bScale * b.y + cScale * c.y + dScale * d.y;
        result.z = aScale * a.z + bScale * b.z + cScale * c.z + dScale * d.z;
    }
#endregion
}