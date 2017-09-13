using UnityEngine;
using System;
using Bezier3D;

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
    /// <summary> Number of steps in the cache. More steps = more accurate approximation </summary>
    public int CacheSteps { get { return _cache.length; } }

    /// <summary> B and C in world coordinates </summary>
    [SerializeField] private Vector3 _B, _C;
    /// <summary> Total length of the curve </summary>
    public float length { get { return _length; } }
    [SerializeField] private float _length;

    public AnimationCurve cache { get { return _cache; } }
    [SerializeField] private AnimationCurve _cache;
    public AnimationCurve reverseCache { get { return _reverseCache; } }
    [SerializeField] private AnimationCurve _reverseCache;
    [SerializeField] private Vector3AnimationCurve _tangentCache;
    [SerializeField] private Vector3AnimationCurve _upCache;
    //[SerializeField] private QuaternionAnimationCurve _orientationCache;


    /// <summary> Constructor </summary>
    /// <param name="a">Start Point</param>
    /// <param name="b">First handle. Local to start point</param>
    /// <param name="c">Second handle. Local to end point</param>
    /// <param name="d">End point</param>
    /// <param name="steps">Number of steps in the cache. More steps = more accurate approximation</param>
    public Bezier3DCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps) {
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _B = a + b;
        _C = d + c;
        GetDistanceCache(a,a+b,c+d,d,steps, out _cache, out _reverseCache);
        _tangentCache = GetTangentCache(a, a + b, c + d, d, steps);
        _length = _cache.keys[_cache.keys.Length - 1].time;
    }

    #region Public methods
    /// <summary> Get point on curve in local coordinates </summary>
    /// <param name="t">Time</param>
    public Vector3 GetPoint(float t) {
		return GetPoint(_a, _B, _C, _d, t);
	}

    /// <summary> Get tangent in local coordinates </summary>
    /// <param name="t">Time</param>
    public Vector3 GetForward(float t) {
        return GetForward(_a, _B, _C, _d, t);
    }

    /// <summary> Approximate tangent in local coordinates </summary>
    /// <param name="t">Time</param>
    public Vector3 GetForwardFast(float t) {
        return _tangentCache.Evaluate(t);
    }

    /// <summary> Approximate up vector in local coordinates </summary>
    /// <param name="t">Time</param>
    public Vector3 GetUpFast(float t) {
        return _upCache == null ? Vector3.up : _upCache.Evaluate(t);

    }

    /// <summary> Approximate orientation in local coordinates </summary>
    /// <param name="t">Time</param>
    public Quaternion GetOrientationFast(float t) {
        Vector3 up = GetUpFast(t);
        Vector3 forward = GetForwardFast(t);
        return Quaternion.LookRotation(forward, up);
    }

    public float Dist2Time(float distance) {
        return _cache.Evaluate(distance);
    }

    public float Time2Dist(float t) {
        return _reverseCache.Evaluate(t);
    }

    public void SetUpCache(Vector3AnimationCurve upCache) {
        _upCache = upCache;
    }
    #endregion

    #region Private methods
    private static Vector3AnimationCurve GetTangentCache(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps) {
        Vector3AnimationCurve curve = new Vector3AnimationCurve();
        float delta = 1f / steps;
        for (int i = 0; i < steps+1; i++) {
            curve.AddKey(delta * i, GetForward(p0, p1, p2, p3, delta * i).normalized);
        }
        return curve;
    }

    private static void GetDistanceCache(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps, out AnimationCurve cache, out AnimationCurve reverseCache) {
        cache = new AnimationCurve(); //time = distance, value = time
        reverseCache = new AnimationCurve(); //time = time, value = distance
        Vector3 prevPos = Vector3.zero;
        float totalLength = 0f;
        for (int i = 0; i <= steps; i++) {
            //Normalize i
            float t = (float)i / (float)steps;
            //Get position from t
            Vector3 newPos = GetPoint(p0, p1, p2, p3, t);
            //First step
            if (i == 0) {
                //Add point at (0,0)
                prevPos = GetPoint(p0, p1, p2, p3, 0);
                cache.AddKey(0, 0);
                reverseCache.AddKey(0, 0);
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
                cache.AddKey(totalLength, t);
                reverseCache.AddKey(t, totalLength);
            }
        }
    }
    public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
		float oneMinusT = 1f - t;
		return
			oneMinusT * oneMinusT * oneMinusT * a +
			3f * oneMinusT * oneMinusT * t * b +
			3f * oneMinusT * t * t * c +
			t * t * t * d;
	}
    private static Vector3 GetForward(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) { //Also known as first derivative
		float oneMinusT = 1f - t;
		return
			3f * oneMinusT * oneMinusT * (b - a) +
			6f * oneMinusT * t * (c - b) +
			3f * t * t * (d - c);
	}
    #endregion
}
