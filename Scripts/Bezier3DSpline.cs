using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[AddComponentMenu("Miscellaneous/Bezier Spline")]
public class Bezier3DSpline : MonoBehaviour {

    public int KnotCount { get { return curves.Length + (closed?0 : 1); } }
    public int CurveCount { get { return curves.Length; } }
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
    [SerializeField] protected Bezier3DCurve[] curves = new Bezier3DCurve[] { new Bezier3DCurve(new Vector3(-1, 0, 0), new Vector3(1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, 0), 60) };
    /// <summary> Automatic knots don't have handles. Instead they have a percentage and adjust their handles accordingly. A percentage of 0 indicates that this is not automatic </summary>
    [SerializeField] protected List<float> autoKnot = new List<float>() { 0, 0 };
    [SerializeField] protected List<NullableQuaternion> orientations = new List<NullableQuaternion>() { new NullableQuaternion(null), new NullableQuaternion(null) };
    [SerializeField] protected Vector3[] tangentCache = new Vector3[0];

#region Public methods

#region Public : get

    public float DistanceToTime(float dist) {
        float t = 0f;
        for (int i = 0; i < CurveCount; i++) {
            if (curves[i].length < dist) {
                dist -= curves[i].length;
                t += 1f / CurveCount;
            } else {
                t += curves[i].Dist2Time(dist) / CurveCount;
                return t;
            }
        }
        return 1f;
    }

    /// <summary> Get <see cref="Bezier3DCurve"/> by index </summary>
    public Bezier3DCurve GetCurve(int i) {
        if (i >= CurveCount || i < 0) throw new System.IndexOutOfRangeException("Cuve index " + i + " out of range");
        return curves[i];
    }

    /// <summary> Return <see cref="Knot"/> info in local coordinates </summary>
    public Knot GetKnot(int i) {
        if (i == 0) {
            if (closed) return new Knot(curves[0].a, curves[CurveCount - 1].c, curves[0].b, autoKnot[i], orientations[i].NullableValue);
            else return new Knot(curves[0].a, Vector3.zero, curves[0].b, autoKnot[i], orientations[i].NullableValue);
        } else if (i == CurveCount) {
            return new Knot(curves[i - 1].d, curves[i - 1].c, Vector3.zero, autoKnot[i], orientations[i].NullableValue);
        } else {
            return new Knot(curves[i].a, curves[i - 1].c, curves[i].b, autoKnot[i], orientations[i].NullableValue);
        }
    }

#region Public get : Forward
    /// <summary> Return forward vector at set distance along the <see cref="Bezier3DSpline"/>. </summary>
    public Vector3 GetForward(float dist) {
        return transform.TransformDirection(GetForwardLocal(dist));
    }

    /// <summary> Return forward vector at set distance along the <see cref="Bezier3DSpline"/> in local coordinates. </summary>
    public Vector3 GetForwardLocal(float dist) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        return curve.GetForward(curve.Dist2Time(dist));
    }

    /// <summary> Return forward vector at set distance along the <see cref="Bezier3DSpline"/>. Uses approximation. </summary>
    public Vector3 GetForwardFast(float dist) {
        return transform.TransformDirection(GetForwardLocalFast(dist));
    }

    /// <summary> Return forward vector at set distance along the <see cref="Bezier3DSpline"/> in local coordinates. Uses approximation. </summary>
    public Vector3 GetForwardLocalFast(float dist) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        return curve.GetForwardFast(curve.Dist2Time(dist));
    }
#endregion

#region Public get : Up
    /// <summary> Return up vector at set distance along the <see cref="Bezier3DSpline"/>. </summary>
    public Vector3 GetUp(float dist) {
        return GetUp(dist, GetForward(dist), false);
    }

    /// <summary> Return up vector at set distance along the <see cref="Bezier3DSpline"/> in local coordinates. </summary>
    public Vector3 GetUpLocal(float dist) {
        return GetUp(dist, GetForward(dist), true);
    }
#endregion

#region Public get : Point
    /// <summary> Return up vector at set distance along the <see cref="Bezier3DSpline"/>. </summary>
    public Vector3 GetPoint(float dist) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        Vector3 result = Vector3.zero;
        curve.GetPoint(curve.Dist2Time(dist), out result);
        return transform.TransformPoint(result);
    }

    /// <summary> Return up vector at set distance along the <see cref="Bezier3DSpline"/>. </summary>
    public void GetPoint(float dist, out Vector3 result) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        curve.GetPoint(curve.Dist2Time(dist), out result);
        result = transform.TransformPoint(result);
    }

    /// <summary> Return point at lerped position where 0 = start, 1 = end </summary>
    public Vector3 GetPointLocal(float dist) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        Vector3 result = Vector3.zero;
        curve.GetPoint(curve.Dist2Time(dist), out result);
        return result;
    }

    /// <summary> Return point at lerped position where 0 = start, 1 = end </summary>
    public void GetPointLocal(float dist, out Vector3 result) {
        Bezier3DCurve curve = GetCurveDistance(dist, out dist);
        curve.GetPoint(curve.Dist2Time(dist), out result);
    }
#endregion

#region Public get : Orientation
    public Quaternion GetOrientation(float dist) {
        Vector3 forward = GetForward(dist);
        Vector3 up = GetUp(dist, forward, false);
        if (forward.sqrMagnitude != 0) return Quaternion.LookRotation(forward, up);
        else return Quaternion.identity;
    }

    public Quaternion GetOrientationFast(float dist) {
        Vector3 forward = GetForwardFast(dist);
        Vector3 up = GetUp(dist, forward, false);
        if (forward.sqrMagnitude != 0) return Quaternion.LookRotation(forward, up);
        else return Quaternion.identity;
    }

    public Quaternion GetOrientationLocal(float dist) {
        Vector3 forward = GetForwardLocal(dist);
        Vector3 up = GetUp(dist, forward, true);
        if (forward.sqrMagnitude != 0) return Quaternion.LookRotation(forward, up);
        else return Quaternion.identity;
    }

    public Quaternion GetOrientationLocalFast(float dist) {
        Vector3 forward = GetForwardLocalFast(dist);
        Vector3 up = GetUp(dist, forward, true);
        if (forward.sqrMagnitude != 0) return Quaternion.LookRotation(forward, up);
        else return Quaternion.identity;
    }
#endregion

#endregion

#region Public : Set
    /// <summary> Setting spline to closed will generate an extra curve, connecting end point to start point </summary>
    public void SetClosed(bool closed) {
        if (closed != _closed) {
            _closed = closed;
            if (closed) {
                List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
                curveList.Add(new Bezier3DCurve(curves[CurveCount - 1].d, -curves[CurveCount - 1].c, -curves[0].b, curves[0].a, cacheDensity));
                curves = curveList.ToArray();
            } else {
                List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
                curveList.RemoveAt(CurveCount - 1);
                curves = curveList.ToArray();
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

    public void RemoveKnot(int i) {
        if (i == 0) {
            Knot knot = GetKnot(1);

            List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
            curveList.RemoveAt(0);
            curves = curveList.ToArray();

            autoKnot.RemoveAt(0);
            orientations.RemoveAt(0);

            SetKnot(0, knot);
        } else if (i == CurveCount) {

            List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
            curveList.RemoveAt(i - 1);
            curves = curveList.ToArray();

            autoKnot.RemoveAt(i);
            orientations.RemoveAt(i);

            if (autoKnot[KnotCount - 1] != 0) SetKnot(KnotCount - 1, GetKnot(KnotCount - 1));
        } else {
            int preCurveIndex, postCurveIndex;
            GetCurveIndicesForKnot(i, out preCurveIndex, out postCurveIndex);

            Bezier3DCurve curve = new Bezier3DCurve(curves[preCurveIndex].a, curves[preCurveIndex].b, curves[postCurveIndex].c, curves[postCurveIndex].d, cacheDensity);

            curves[preCurveIndex] = curve;

            List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
            curveList.RemoveAt(postCurveIndex);
            curves = curveList.ToArray();

            autoKnot.RemoveAt(i);
            orientations.RemoveAt(i);

            int preKnotIndex, postKnotIndex;
            GetKnotIndicesForKnot(i, out preKnotIndex, out postKnotIndex);

            SetKnot(preKnotIndex, GetKnot(preKnotIndex));
        }
    }

    public void AddKnot(Knot knot) {
        Bezier3DCurve curve = new Bezier3DCurve(curves[CurveCount - 1].d, -curves[CurveCount - 1].c, knot.handleIn, knot.position, cacheDensity);

        List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
        curveList.Add(curve);
        curves = curveList.ToArray();

        autoKnot.Add(knot.auto);
        orientations.Add(knot.orientation);
        SetKnot(KnotCount - 1, knot);
    }

    public void InsertKnot(int i, Knot knot) {
        Bezier3DCurve curve;
        if (i == 0) curve = new Bezier3DCurve(knot.position, knot.handleOut, -curves[0].b, curves[0].a, cacheDensity);
        else if (i == CurveCount) curve = GetCurve(i - 1);
        else curve = GetCurve(i);

        List<Bezier3DCurve> curveList = new List<Bezier3DCurve>(curves);
        curveList.Insert(i, curve);
        curves = curveList.ToArray();

        autoKnot.Insert(i, knot.auto);
        orientations.Insert(i, knot.orientation);
        SetKnot(i, knot);
    }

    /// <summary> Set Knot info in local coordinates </summary>
    public void SetKnot(int i, Knot knot) {
        //If knot is set to auto, adjust handles accordingly
        orientations[i] = knot.orientation;
        autoKnot[i] = knot.auto;
        if (knot.auto != 0) AutomateHandles(i, ref knot);

        //Automate knots around this knot
        int preKnotIndex, postKnotIndex;
        GetKnotIndicesForKnot(i, out preKnotIndex, out postKnotIndex);

        Knot preKnot = new Knot();
        if (preKnotIndex != -1) {
            preKnot = GetKnot(preKnotIndex);
            if (preKnot.auto != 0) {
                int preKnotPreCurveIndex, preKnotPostCurveIndex;
                GetCurveIndicesForKnot(preKnotIndex, out preKnotPreCurveIndex, out preKnotPostCurveIndex);
                if (preKnotPreCurveIndex != -1) {
                    AutomateHandles(preKnotIndex, ref preKnot, curves[preKnotPreCurveIndex].a, knot.position);
                    curves[preKnotPreCurveIndex] = new Bezier3DCurve(curves[preKnotPreCurveIndex].a, curves[preKnotPreCurveIndex].b, preKnot.handleIn, preKnot.position, cacheDensity);
                } else {
                    AutomateHandles(preKnotIndex, ref preKnot, Vector3.zero, knot.position);
                }
            }
        }

        Knot postKnot = new Knot();
        if (postKnotIndex != -1) {
            postKnot = GetKnot(postKnotIndex);
            if (postKnot.auto != 0) {
                int postKnotPreCurveIndex, postKnotPostCurveIndex;
                GetCurveIndicesForKnot(postKnotIndex, out postKnotPreCurveIndex, out postKnotPostCurveIndex);
                if (postKnotPostCurveIndex != -1) {
                    AutomateHandles(postKnotIndex, ref postKnot, knot.position, curves[postKnotPostCurveIndex].d);
                    curves[postKnotPostCurveIndex] = new Bezier3DCurve(postKnot.position, postKnot.handleOut, curves[postKnotPostCurveIndex].c, curves[postKnotPostCurveIndex].d, cacheDensity);
                } else {
                    AutomateHandles(postKnotIndex, ref postKnot, knot.position, Vector3.zero);
                }
            }
        }

        //Get the curve indices in direct contact with knot
        int preCurveIndex, postCurveIndex;
        GetCurveIndicesForKnot(i, out preCurveIndex, out postCurveIndex);

        //Adjust curves in direct contact with the knot
        if (preCurveIndex != -1) curves[preCurveIndex] = new Bezier3DCurve(preKnot.position, preKnot.handleOut, knot.handleIn, knot.position, cacheDensity);
        if (postCurveIndex != -1) curves[postCurveIndex] = new Bezier3DCurve(knot.position, knot.handleOut, postKnot.handleIn, postKnot.position, cacheDensity);

        _totalLength = GetTotalLength();

    }

    /// <summary> Flip the spline </summary>
    public void Flip() {
        Bezier3DCurve[] curves = new Bezier3DCurve[CurveCount];
        for (int i = 0; i < CurveCount; i++) {
            curves[CurveCount - 1 - i] = new Bezier3DCurve(this.curves[i].d, this.curves[i].c, this.curves[i].b, this.curves[i].a, cacheDensity);
        }
        this.curves = curves;
        autoKnot.Reverse();
        orientations.Reverse();
    }
#endregion

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
    private Vector3 GetUp(float dist, Vector3 tangent, bool local) {
        float t = DistanceToTime(dist);
        t *= CurveCount;

        Quaternion rot_a = Quaternion.identity, rot_b = Quaternion.identity;
        int t_a = 0, t_b = 0;

        //Find preceding rotation
        for (int i = Mathf.Min((int) t, CurveCount); i >= 0; i--) {
            i = (int) Mathf.Repeat(i, KnotCount - 1);
            if (orientations[i].HasValue) {
                rot_a = orientations[i].Value;
                rot_b = orientations[i].Value;
                t_a = i;
                t_b = i;
                break;
            }
        }
        //Find proceding rotation
        for (int i = Mathf.Max((int) t + 1, 0); i < orientations.Count; i++) {
            if (orientations[i].HasValue) {
                rot_b = orientations[i].Value;
                t_b = i;
                break;
            }
        }
        t = Mathf.InverseLerp(t_a, t_b, t);
        Quaternion rot = Quaternion.Lerp(rot_a, rot_b, t);
        if (!local) rot = transform.rotation * rot;
        //Debug.Log(t_a + " / " + t_b + " / " + t);
        return Vector3.ProjectOnPlane(rot * Vector3.up, tangent).normalized;
    }

    /// <summary> Get the curve indices in direct contact with knot </summary>
    private void GetCurveIndicesForKnot(int knotIndex, out int preCurveIndex, out int postCurveIndex) {
        //Get the curve index in direct contact with, before the knot
        preCurveIndex = -1;
        if (knotIndex != 0) preCurveIndex = knotIndex - 1;
        else if (closed) preCurveIndex = CurveCount - 1;

        //Get the curve index in direct contact with, after the knot
        postCurveIndex = -1;
        if (knotIndex != CurveCount) postCurveIndex = knotIndex;
        else if (closed) postCurveIndex = 0;
    }

    /// <summary> Get the knot indices in direct contact with knot </summary>
    private void GetKnotIndicesForKnot(int knotIndex, out int preKnotIndex, out int postKnotIndex) {
        //Get the curve index in direct contact with, before the knot
        preKnotIndex = -1;
        if (knotIndex != 0) preKnotIndex = knotIndex - 1;
        else if (closed) preKnotIndex = KnotCount - 1;

        //Get the curve index in direct contact with, after the knot
        postKnotIndex = -1;
        if (knotIndex != KnotCount - 1) postKnotIndex = knotIndex + 1;
        else if (closed) postKnotIndex = 0;
    }

    private Bezier3DCurve GetCurve(float splineT, out float curveT) {
        splineT *= CurveCount;
        for (int i = 0; i < CurveCount; i++) {
            if (splineT > 1f) splineT -= 1f;
            else {
                curveT = splineT;
                return curves[i];
            }
        }
        curveT = 1f;
        return curves[CurveCount - 1];
    }

    private Bezier3DCurve GetCurveDistance(float splineDist, out float curveDist) {
        for (int i = 0; i < CurveCount; i++) {
            if (curves[i].length < splineDist) splineDist -= curves[i].length;
            else {
                curveDist = splineDist;
                return curves[i];
            }
        }
        curveDist = curves[CurveCount - 1].length;
        return curves[CurveCount - 1];
    }

    /// <summary> Automate handles based on previous and next point positions </summary>
    private void AutomateHandles(int i, ref Knot knot) {
        //Terminology: Points are referred to as A B and C
        //A = prev point, B = current point, C = next point

        Vector3 prevPos;
        if (i != 0) prevPos = curves[i - 1].a;
        else if (closed) prevPos = curves[CurveCount - 1].a;
        else prevPos = Vector3.zero;

        Vector3 nextPos;
        if (i != KnotCount - 1) nextPos = curves[i].d;
        else if (closed) nextPos = curves[0].a;
        else nextPos = Vector3.zero;

        AutomateHandles(i, ref knot, prevPos, nextPos);
    }

    /// <summary> Automate handles based on previous and next point positions </summary>
    private void AutomateHandles(int i, ref Knot knot, Vector3 prevPos, Vector3 nextPos) {
        //Terminology: Points are referred to as A B and C
        //A = prev point, B = current point, C = next point
        float amount = knot.auto;

        //Calculate directional vectors
        Vector3 AB = knot.position - prevPos;
        Vector3 CB = knot.position - nextPos;
        //Calculate the across vector
        Vector3 AB_CB = (CB.normalized - AB.normalized).normalized;

        if (!closed) {
            if (i == 0) {
                knot.handleOut = CB * -amount;
            } else if (i == CurveCount) {
                knot.handleIn = AB * -amount;
            } else {
                knot.handleOut = -AB_CB * CB.magnitude * amount;
                knot.handleIn = AB_CB * AB.magnitude * amount;
            }
        } else {
            if (KnotCount == 2) {
                Vector3 left = new Vector3(AB.z, 0, -AB.x) * amount;
                if (i == 0) {
                    knot.handleIn = left;
                    knot.handleOut = -left;
                }
                if (i == 1) {
                    knot.handleIn = left;
                    knot.handleOut = -left;
                }
            } else {
                knot.handleIn = AB_CB * AB.magnitude * amount;
                knot.handleOut = -AB_CB * CB.magnitude * amount;
            }
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

    /// <summary> Unity doesn't support serialization of nullable types, so here's a custom struct that does exactly the same thing </summary>
    [Serializable]
    protected struct NullableQuaternion {
        public Quaternion Value { get { return rotation; } }
        public Quaternion? NullableValue {
            get {
                if (hasValue) return rotation;
                else return null;
            }
        }
        public bool HasValue { get { return hasValue; } }

        [SerializeField] private Quaternion rotation;
        [SerializeField] private bool hasValue;

        public NullableQuaternion(Quaternion? rot) {
            rotation = rot.HasValue?rot.Value : Quaternion.identity;
            hasValue = rot.HasValue;
        }

        //  User-defined conversion from nullable type to NullableQuaternion
        public static implicit operator NullableQuaternion(Quaternion? r) {
            return new NullableQuaternion(r);
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmos() {
        //Set color depending on selection
        if (Array.IndexOf(UnityEditor.Selection.gameObjects, gameObject) >= 0) {
            Gizmos.color = Color.yellow;
        } else Gizmos.color = new Color(1, 0.6f, 0f);

        Vector3 prev, cur;
        //Loop through each curve in spline
        for (int i = 0; i < CurveCount; i++) {
            Bezier3DCurve curve = GetCurve(i);

            //Get curve in world space
            Vector3 a, b, c, d;
            a = transform.TransformPoint(curve.a);
            b = transform.TransformPoint(curve.b + curve.a);
            c = transform.TransformPoint(curve.c + curve.d);
            d = transform.TransformPoint(curve.d);

            int segments = 50;
            float spacing = 1f / segments;
            Bezier3DCurve.GetPoint(ref a, ref b, ref c, ref d, 0f, out prev);
            for (int k = 0; k <= segments; k++) {
                Bezier3DCurve.GetPoint(ref a, ref b, ref c, ref d, k * spacing, out cur);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
    }
#endif
}