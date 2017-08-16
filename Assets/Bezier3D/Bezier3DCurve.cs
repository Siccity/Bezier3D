using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable Bezier curve between two points
/// </summary>
[System.Serializable]
public struct Bezier3DCurve {


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

    public AnimationCurve cache { get { return _cache; } }
    [SerializeField] private AnimationCurve _cache;

    /// <summary> Constructor </summary>
	public Bezier3DCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps) {
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _B = a + b;
        _C = d + c;
        _cache = GetDistanceCache(a,a+b,c+d,d,steps);
        _length = _cache.keys[_cache.keys.Length - 1].time;
    }

    #region Public methods
    public Vector3 GetPointTime(float t) {
		return GetPointTime(_a, _B, _C, _d, t);
	}

    public Vector3 GetPointDistance(float distance) {
        return GetPointTime(_a, _B, _C, _d, Dist2Time(distance));
    }

    public float Dist2Time(float distance) {
        return _cache.Evaluate(distance);
    }
    #endregion

    #region Private methods
    private static AnimationCurve GetDistanceCache(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps) {
        AnimationCurve curve = new AnimationCurve(); //time = distance, value = time
        Vector3 prevPos = Vector3.zero;
        float totalLength = 0f;
        for (int i = 0; i <= steps; i++) {
            //Normalize i
            float t = (float)i / (float)steps;
            //Get position from t
            Vector3 newPos = GetPointTime(p0, p1, p2, p3, t);
            //First step
            if (i == 0) {
                //Add point at (0,0)
                prevPos = GetPointTime(p0, p1, p2, p3, 0);
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

    public static Vector3 GetPointTime(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
		t = Mathf.Clamp01(t);
		float oneMinusT = 1f - t;
		return
			oneMinusT * oneMinusT * oneMinusT * a +
			3f * oneMinusT * oneMinusT * t * b +
			3f * oneMinusT * t * t * c +
			t * t * t * d;
	}

    private static Vector3 GetFirstDerivative(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
		t = Mathf.Clamp01(t);
		float oneMinusT = 1f - t;
		return
			3f * oneMinusT * oneMinusT * (b - a) +
			6f * oneMinusT * t * (c - b) +
			3f * t * t * (d - c);
	}
    #endregion
}
