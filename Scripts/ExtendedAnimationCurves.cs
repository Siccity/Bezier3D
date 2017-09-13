using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

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

        private Keyframe[] keys = new Keyframe[0];

        private struct Keyframe : IComparable<Keyframe> {
            public Quaternion value;
            public float time;

            public Keyframe(float time, Quaternion value) {
                this.time = time;
                this.value = value;
            }

            public int CompareTo(Keyframe other) {
                return time.CompareTo(other.time);
            }
        }

        /// <summary> The number of keys in the curve (Read Only) </summary>
        public int length { get { return keys.Length; } }

        public Quaternion Evaluate(float time) {
            if (keys.Length == 0) return Quaternion.identity;

            int len = length;
            Quaternion a = keys[0].value;
            Quaternion b = keys[0].value;
            for (int i = 0; i < len; i++) {
                Quaternion c = keys[i].value;
                if (keys[i].time > time) {
                    if (i < len-1) {
                        float t = Mathf.InverseLerp(keys[i].time, keys[i+1].time, time);
                        Quaternion d = keys[i+1].value;
                        return Quaternion.Lerp(
                            Quaternion.Lerp(
                                Quaternion.Lerp(a, b, t),
                                Quaternion.Lerp(b, c, t),
                                t),
                            Quaternion.Lerp(
                                Quaternion.Lerp(b,c,t),
                                Quaternion.Lerp(c,d,t),
                                t),
                            t);
                    }
                }
                a = b;
                b = c;
            }
            return keys[keys.Length-1].value;
        }

        public void AddKey(float time, Quaternion value) {
            List<Keyframe> qList = new List<Keyframe>(keys);
            qList.Add(new Keyframe(time, value));
            qList.Sort();
            keys = qList.ToArray();
        }

        /// <summary> Gets the rotation of the last key </summary>
        public Quaternion EvaluateEnd() {
            return keys[keys.Length - 1].value;
        }
    }

    /// <summary> Similar to AnimationCurve, except all values are constant. No smoothing applied between keys </summary>
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

    /// <summary> Animation curve which stores quaternions, and can evaluate smoothed values in between keyframes </summary>
    [System.Serializable]
    public class Vector3AnimationCurve {
        [SerializeField]
        private AnimationCurve
            xV = new AnimationCurve(),
            yV = new AnimationCurve(),
            zV = new AnimationCurve();

        /// <summary> The number of keys in the curve (Read Only) </summary>
        public int length { get { return xV.length; } }

        public Vector3 Evaluate(float time) {
            return new Vector3(xV.Evaluate(time), yV.Evaluate(time), zV.Evaluate(time));
        }

        public void AddKey(float time, Vector3 value) {
            xV.AddKey(time, value.x);
            yV.AddKey(time, value.y);
            zV.AddKey(time, value.z);
        }

        /// <summary> Gets the rotation of the last key </summary>
        public Vector3 EvaluateEnd() {
            return GetKeyValue(xV.length - 1);
        }

        public float GetKeyTime(int keyIndex) {
            return xV.keys[keyIndex].time;
        }

        public Vector3 GetKeyValue(int keyIndex) {
            return new Vector3(xV.keys[keyIndex].value, yV.keys[keyIndex].value, zV.keys[keyIndex].value);
        }
        public Vector3AnimationCurve() {

        }
    }
}