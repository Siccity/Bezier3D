using UnityEngine;
using System.Collections.Generic;

namespace Bezier3D {
    /// <summary>
    /// Class Extensions
    /// </summary>
    public static class ExtendedAnimationCurves {
        public static void Serialize(this AnimationCurve anim, out float[] times, out float[] values) {
            times = new float[anim.length];
            values = new float[anim.length];
            for (int i = 0; i < anim.length; i++) {
                times[i] = anim.keys[i].time;
                values[i] = anim.keys[i].value;
            }
        }
        public static AnimationCurve Deserialize(float[] times, float[] values) {
            AnimationCurve anim = new AnimationCurve();
            if (times.Length != values.Length) Debug.LogWarning("Input data lengths do not match");
            else {
                for (int i = 0; i < times.Length; i++) anim.AddKey(new Keyframe(times[i], values[i]));
            }
            return anim;
        }
    }

    /// <summary>
    /// Animation curve which stores quaternions, and can evaluate smoothed values in between keyframes
    /// </summary>
    [System.Serializable]
    public class QuaternionAnimationCurve {
        [SerializeField]
        private AnimationCurve
            xQ = new AnimationCurve(),
            yQ = new AnimationCurve(),
            zQ = new AnimationCurve(),
            wQ = new AnimationCurve();

        /// <summary>
        /// The number of keys in the curve (Read Only)
        /// </summary>
        public int length { get { return xQ.length; } }

        public Quaternion Evaluate(float time) {
            return new Quaternion(xQ.Evaluate(time), yQ.Evaluate(time), zQ.Evaluate(time), wQ.Evaluate(time));
        }

        public QuaternionAnimationCurve() { }

        public QuaternionAnimationCurve(Serializable serialized) {
            xQ = ExtendedAnimationCurves.Deserialize(serialized.xT, serialized.xV);
            yQ = ExtendedAnimationCurves.Deserialize(serialized.yT, serialized.yV);
            zQ = ExtendedAnimationCurves.Deserialize(serialized.zT, serialized.zV);
            wQ = ExtendedAnimationCurves.Deserialize(serialized.wT, serialized.wV);
        }
        public void AddKey(float time, Quaternion value) {
            xQ.AddKey(time, value.x);
            yQ.AddKey(time, value.y);
            zQ.AddKey(time, value.z);
            wQ.AddKey(time, value.w);
        }

        /// <summary>
        /// Gets the rotation of the last key
        /// </summary>
        public Quaternion EvaluateEnd() {
            return GetKeyValue(xQ.length - 1);
        }

        public float GetKeyTime(int keyIndex) {
            return wQ.keys[keyIndex].time;
        }

        public Quaternion GetKeyValue(int keyIndex) {
            return new Quaternion(xQ.keys[keyIndex].value, yQ.keys[keyIndex].value, zQ.keys[keyIndex].value, wQ.keys[keyIndex].value);
        }

        [System.Serializable]
        public class Serializable {
            public Serializable(QuaternionAnimationCurve curve) {
                curve.xQ.Serialize(out xT, out xV);
                curve.yQ.Serialize(out yT, out yV);
                curve.zQ.Serialize(out zT, out zV);
                curve.wQ.Serialize(out wT, out wV);
            }
            public float[] xT, xV, yT, yV, zT, zV, wT, wV;
        }
    }

    /// <summary>
    /// Similar to AnimationCurve, except all values are constant. No smoothing applied between keys
    /// </summary>
    [System.Serializable]
    public class ConstantAnimationCurve {
        [SerializeField]
        List<float> _time = new List<float>();
        [SerializeField]
        List<float> _value = new List<float>();

        /// <summary>
        /// The number of keys in the curve (Read Only)
        /// </summary>
        public int length { get { return _time.Count; } }

        public float Evaluate(float time) {
            if (length == 0) return 0;
            float returnValue = GetKeyValue(0);
            for (int i = 0; i < _time.Count; i++) {
                if (_time[i] <= time) returnValue = _value[i];
                else break;
            }
            return returnValue;
        }

        public void AddKey(float time, float value) {
            for (int i = 0; i < _time.Count; i++) {
                if (_time[i] > time) {
                    _time.Insert(i, time);
                    _value.Insert(i, value);
                    return;
                }
                else if (_time[i] == time) {
                    _time[i] = time;
                    _value[i] = value;
                    return;
                }
            }
            _time.Add(time);
            _value.Add(value);
        }

        /// <summary>
        /// Gets the last value
        /// </summary>
        public float EvaluateEnd() {
            return _value[_value.Count - 1];
        }

        public float GetKeyTime(int keyIndex) {
            return _time[keyIndex];
        }

        public float GetKeyValue(int keyIndex) {
            return _value[keyIndex];
        }
    }

    /// <summary>
    /// Animation curve which stores quaternions, and can evaluate smoothed values in between keyframes
    /// </summary>
    [System.Serializable]
    public class Vector3AnimationCurve {
        [SerializeField]
        private AnimationCurve
            xV = new AnimationCurve(),
            yV = new AnimationCurve(),
            zV = new AnimationCurve();

        /// <summary>
        /// The number of keys in the curve (Read Only)
        /// </summary>
        public int length { get { return xV.length; } }

        public Vector3 Evaluate(float time) {
            return new Vector3(xV.Evaluate(time), yV.Evaluate(time), zV.Evaluate(time));
        }

        public void AddKey(float time, Vector3 value) {
            xV.AddKey(time, value.x);
            yV.AddKey(time, value.y);
            zV.AddKey(time, value.z);
        }

        /// <summary>
        /// Gets the rotation of the last key
        /// </summary>
        public Vector3 EvaluateEnd() {
            return GetKeyValue(xV.length - 1);
        }

        public float GetKeyTime(int keyIndex) {
            return xV.keys[keyIndex].time;
        }

        public Vector3 GetKeyValue(int keyIndex) {
            return new Vector3(xV.keys[keyIndex].value, yV.keys[keyIndex].value, zV.keys[keyIndex].value);
        }

        public Vector3AnimationCurve() { }

        public Vector3AnimationCurve(Serializable serialized) {
            xV = ExtendedAnimationCurves.Deserialize(serialized.xT, serialized.xV);
            yV = ExtendedAnimationCurves.Deserialize(serialized.yT, serialized.yV);
            zV = ExtendedAnimationCurves.Deserialize(serialized.zT, serialized.zV);
        }

        [System.Serializable]
        public class Serializable {
            public Serializable(Vector3AnimationCurve curve) {
                curve.xV.Serialize(out xT, out xV);
                curve.yV.Serialize(out yT, out yV);
                curve.zV.Serialize(out zT, out zV);
            }
            public float[] xT, xV, yT, yV, zT, zV;
        }
    }
}