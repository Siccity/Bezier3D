using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
public class Bezier3DSpline : MonoBehaviour{

    /// <summary> Callback for when the spline changes </summary>
    public Action onChanged;
	public int KnotCount { get { return curves.Count+(closed?0:1); } }
	public int CurveCount { get { return curves.Count; } }
    /// <summary> Interpolation steps per curve </summary>
    public int cacheDensity { get { return _cacheDensity; } }
    [SerializeField] protected int _cacheDensity = 60;
    /// <summary> Whether the end of the spline connects to the start of the spline </summary>
    public bool closed { get { return _closed; } }
    [SerializeField] protected bool _closed = false;
    /// <summary> Sum of all curve lengths </summary>
    public float totalLength { get { return _totalLength; } }
    [SerializeField] protected float _totalLength = 2.370671f;
    /// <summary> Curves of the spline </summary>
    [SerializeField] protected List<Bezier3DCurve> curves = new List<Bezier3DCurve>() { new Bezier3DCurve( new Vector3(-1,0,0), new Vector3(1,0,1), new Vector3(-1,0,-1), new Vector3(1,0,0), 60)};
    /// <summary> Automatic knots don't have handles. Instead they have a percentage and adjust their handles accordingly </summary>
    [SerializeField] protected List<float> autoKnot = new List<float>() { 0, 0 };
    [SerializeField] protected List<Quaternion?> orientations = new List<Quaternion?>() { null, null };

    #region Public methods
    /// <summary> Setting spline to closed will generate an extra curve, connecting end point to start point </summary>
    public void SetClosed(bool closed) {
        if (closed != _closed) {
            _closed = closed;
            if (closed) {
                curves.Add(new Bezier3DCurve(curves[CurveCount - 1].d, -curves[CurveCount - 1].c, -curves[0].b, curves[0].a,cacheDensity));
            } else {
                curves.RemoveAt(CurveCount - 1);
            }
            _totalLength = GetTotalLength();
        }
    }

    /// <summary> Recache all individual curves with new step amount </summary> 
    /// <param name="density"> Number of steps per curve </param>
    public void SetCacheDensity(int steps) {
        _cacheDensity = steps;
        for (int i = 0; i < CurveCount; i++) {
            curves[i] = new Bezier3DCurve(curves[i].a, curves[i].b, curves[i].c, curves[i].d, _cacheDensity);
        }
        _totalLength = GetTotalLength();
    }

    /// <summary> Return point in relation to world </summary>
	public Vector3 GetPoint(float t) {
        for (int i = 0; i < CurveCount; i++) {
            if (t > 1f) t -= 1f;
            else return transform.TransformPoint(curves[i].GetPointTime(t));
        }
        return transform.TransformPoint(curves[CurveCount - 1].GetPointTime(1f));
    }

    /// <summary> Return point in relation to world </summary>
    public Vector3 GetPointInterpolated(float dist) {
        for (int i = 0; i < CurveCount; i++) {
            if (curves[i].length < dist) dist -= curves[i].length;
            else return transform.TransformPoint(curves[i].GetPointDistance(dist));
        }
        return transform.TransformPoint(curves[CurveCount - 1].GetPointTime(1f));
    }

    /// <summary> Get curve by index </summary>
	public Bezier3DCurve GetCurve(int i) {
        if (i>CurveCount) throw new System.IndexOutOfRangeException("Cuve index " + i + " out of range");
		return curves[i];
	}

    public void RemoveKnot(int i) {
        if (i == 0) {
            curves.RemoveAt(0);
        } else if (i == CurveCount) {
            curves.RemoveAt(i - 1);
        } else {
            Bezier3DCurve curve = curves[i];
            curves.RemoveAt(i);
            autoKnot.RemoveAt(i);
            orientations.RemoveAt(i);
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, curve.c, curve.d,cacheDensity);
        }
        _totalLength = GetTotalLength();
        if (onChanged != null) onChanged();
    }

    public void AddKnot(Knot knot) {
        curves.Add(new Bezier3DCurve(curves[CurveCount-1].d, -curves[CurveCount - 1].c, knot.handleIn, knot.position, cacheDensity));
        autoKnot.Add(autoKnot[autoKnot.Count-1]);
        orientations.Add(null);
        _totalLength = GetTotalLength();
        if (onChanged != null) onChanged();
    }

    public void InsertKnot(int i, Knot knot) {
        if (i == 0) {
            Bezier3DCurve curve = GetCurve(0);
            Bezier3DCurve newCurve = new Bezier3DCurve(knot.position, knot.handleOut, -curve.b, curve.a, cacheDensity);
            curves.Insert(i, newCurve);
            autoKnot.Insert(i, autoKnot[i]);
            orientations.Insert(i, null);
        } else if (i == CurveCount) {
            curves.Add(new Bezier3DCurve(knot.position, knot.handleOut, curves[i - 1].c, curves[i - 1].d, cacheDensity));
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, knot.handleIn, knot.position, cacheDensity);
            autoKnot.Add(autoKnot[autoKnot.Count - 1]);
            orientations.Add(null);
        } else {
            curves.Insert(i, new Bezier3DCurve(knot.position, knot.handleOut, curves[i - 1].c, curves[i - 1].d, cacheDensity));
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, knot.handleIn, knot.position, cacheDensity);
            autoKnot.Insert(i, autoKnot[i]);
            orientations.Insert(i, null);
        }
        _totalLength = GetTotalLength();
        if (onChanged != null) onChanged();
    }

    /// <summary> Set Knot info in local coordinates </summary>
    public void SetKnot(int i, Knot knot) {
        if (i == 0) {
            if (closed) curves[CurveCount-1] = new Bezier3DCurve(curves[CurveCount-1].a, curves[CurveCount - 1].b, knot.handleIn, knot.position, cacheDensity);
            curves[0] = new Bezier3DCurve(knot.position, knot.handleOut, curves[0].c, curves[0].d, cacheDensity);
        } else if (i == CurveCount) {
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, knot.handleIn, knot.position, cacheDensity);
        } else {
            curves[i] = new Bezier3DCurve(knot.position, knot.handleOut, curves[i].c, curves[i].d, cacheDensity);
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, knot.handleIn, knot.position, cacheDensity);
        }

        autoKnot[i] = knot.auto;
        orientations[i] = knot.orientation;
        if (knot.auto > 0) {
            AutomateKnot(i);

            if (i != 0) AutomateKnot(i - 1);
            else if (closed) AutomateKnot(KnotCount - 1);

            if (i != KnotCount - 1) AutomateKnot(i + 1);
            else if (closed) AutomateKnot(0);
        }
        _totalLength = GetTotalLength();
        if (onChanged != null) onChanged();
    }

    /// <summary> Return Knot info in local coordinates </summary>
    public Knot GetKnot(int i) {
        if (i == 0) {
            if (closed) return new Knot(curves[0].a, curves[CurveCount-1].c, curves[0].b, autoKnot[i], orientations[i]);
            else return new Knot(curves[0].a, Vector3.zero, curves[0].b, autoKnot[i], orientations[i]);
        } else if (i == CurveCount) {
            return new Knot(curves[i - 1].d, curves[i - 1].c, Vector3.zero, autoKnot[i], orientations[i]);
        } else {
            return new Knot(curves[i].a, curves[i - 1].c, curves[i].b,autoKnot[i], orientations[i]);
        }
    }

    public Quaternion GetOrientationInterpolated(float dist) {
        return Quaternion.identity;
        Quaternion firstRot;
        float firstRotPos;
        Quaternion lastRot;
        float lastRotPos;
        for (int i = 0; i < CurveCount; i++) {
            if (curves[i].length < dist) dist -= curves[i].length;
            //else return transform.TransformPoint(curves[i].GetPointDistance(dist));
        }
    }
    #endregion

    public struct Knot {
        public Vector3 position;
        public Vector3 handleIn;
        public Vector3 handleOut;
        public float auto;
        public Quaternion? orientation;

        /// <summary> Constructor </summary>
        /// <param name="position">Position of the knot local to spline transform</param>
        /// <param name="handleIn">Left handle position local to knot position</param>
        /// <param name="handleOut">Right handle position local to knot position</param>
        /// <param name="automatic">Any value above 0 will result in an automatically configured knot (ignoring handle inputs)</param>
        public Knot(Vector3 position, Vector3 handleIn, Vector3 handleOut, float automatic = 0f, Quaternion? orientation = null) {
            this.position = position;
            this.handleIn = handleIn;
            this.handleOut = handleOut;
            this.auto = automatic;
            this.orientation = orientation;
        }
    }

    #region Private methods
    private void AutomateKnot(int i) {
        float amount = autoKnot[i];
        if (amount == 0f) return;

        Knot knot = GetKnot(i);

        Vector3 prevPos;
        if (i != 0) prevPos = curves[i - 1].a;
        else if (closed) prevPos = curves[CurveCount - 1].a;
        else prevPos = Vector3.zero;

        Vector3 nextPos;
        if (i != KnotCount - 1) nextPos = curves[i].d;
        else if (closed) nextPos = curves[0].a;
        else nextPos = Vector3.zero;

        Vector3 np = (knot.position - prevPos).normalized;
        Vector3 pp = (knot.position - nextPos).normalized;
        Vector3 mp = Vector3.Lerp(np, pp, 0.5f);
        Vector3 norm = Vector3.Cross(np, pp).normalized;
        Quaternion rot = Quaternion.AngleAxis(90, norm);
        mp = rot * mp;
        
        Vector3 ab = mp.normalized * amount;
        if (i == 0) {
            if (closed) curves[CurveCount - 1] = new Bezier3DCurve(curves[CurveCount - 1].a, curves[CurveCount - 1].b, ab, knot.position, cacheDensity);
            curves[0] = new Bezier3DCurve(knot.position, -ab, curves[0].c, curves[0].d, cacheDensity);
        } else if (i == KnotCount-1) {
            if (closed) curves[CurveCount - 1] = new Bezier3DCurve(knot.position, -ab, curves[CurveCount - 1].c, curves[CurveCount - 1].d, cacheDensity);
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, ab, knot.position, cacheDensity);
        } else {
            curves[i] = new Bezier3DCurve(knot.position, -ab, curves[i].c, curves[i].d, cacheDensity);
            curves[i - 1] = new Bezier3DCurve(curves[i - 1].a, curves[i - 1].b, ab, knot.position, cacheDensity);
        }
    }

    private float GetTotalLength() {
        float length = 0f;
        for (int i = 0; i < CurveCount; i++) {
            length += curves[i].length;
        }
        return length;
    }
    #endregion

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (Array.IndexOf(UnityEditor.Selection.gameObjects, gameObject) >= 0) {
            Gizmos.color = Color.yellow;
        } else  Gizmos.color = new Color(1, 0.6f, 0f);
        for (int i = 0; i < CurveCount; i++) {
            Bezier3DCurve curve = GetCurve(i);
            Vector3 a, b, c, d;
            a = transform.TransformPoint(curve.a);
            b = transform.TransformPoint(curve.b + curve.a);
            c = transform.TransformPoint(curve.c + curve.d);
            d = transform.TransformPoint(curve.d);
            Vector3 prev = Bezier3DCurve.GetPointTime(a, b, c, d, 0f);
            for (float t = 0f; t < 1f; t += 0.02f) {
                Vector3 cur = Bezier3DCurve.GetPointTime(a, b, c, d, t + 0.02f);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
    }
#endif
}
