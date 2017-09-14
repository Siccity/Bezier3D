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

    /// <summary> Animation curve which stores quaternions, and can evaluate smoothed values in between keyframes </summary>
    [Serializable]
    public class QuaternionAnimationCurve {

        [SerializeField]
        private QuaternionKeyframe[] keys = new QuaternionKeyframe[0];

        /// <summary> The number of keys in the curve (Read Only) </summary>
        public int length { get { return keys.Length; } }

        public QuaternionAnimationCurve (QuaternionKeyframe[] keys) {
            this.keys = keys;
        }

        public Quaternion EvaluateCubic(float time) {
            if (keys.Length == 0) return Quaternion.identity;
            else if (keys.Length == 1) return keys[0].value;
            else if (keys.Length == 2) {
                if (time <= keys[0].time) return keys[0].value;
                else if (time >= keys[1].time) return keys[1].value;
                else {
                    float t = Mathf.InverseLerp(keys[0].time, keys[1].time, time);
                    return Quaternion.Lerp(keys[0].value, keys[1].value, t);
                }
            }
            else {

                int len = length;
                for (int i = 0; i < len; i++) {
                    if (keys[i].time > time) {
                        int t0Index;
                        if (i > 1) t0Index = i - 2;
                        if (i > 0) t0Index = i - 1;
                        else t0Index = i;

                        int p0Index = i;
                        if (i > 0) p0Index = i - 1;
                        else p0Index = i;

                        int p1Index = i;

                        int t1Index;
                        if (i < len - 1) t1Index = i + 1;
                        else t1Index = i;

                        Quaternion p0 = keys[p0Index].value;
                        Quaternion p1 = keys[p1Index].value;
                        Quaternion t0 = keys[t0Index].value;
                        Quaternion t1 = keys[t1Index].value;

                        //Debug.Log(keys.Length + " " + keys[0].time + " " + keys[1].time + " " + keys[2].time + " " + time);


                        float t = Mathf.InverseLerp(keys[p0Index].time, keys[p1Index].time, time);
                        return Quaternion.Lerp(
                            Quaternion.Lerp(
                                Quaternion.Lerp(p0, t0, t),
                                Quaternion.Lerp(t0, t1, t),
                                t),
                            Quaternion.Lerp(
                                Quaternion.Lerp(t0, t1, t),
                                Quaternion.Lerp(t1, p1, t),
                                t),
                            t);
                    }
                }
                //Debug.Log("noneFound "+keys.Length + " " + keys[0].time + " " + keys[1].time + " " + keys[2].time + " " + time);
                return keys[keys.Length - 1].value;
            }
        }

        public Quaternion EvaluateLinear(float time) {
            if (keys.Length == 0) return Quaternion.identity;
            else if (keys.Length == 1) return keys[0].value;
            else if (keys.Length == 2) {
                if (time <= keys[0].time) return keys[0].value;
                else if (time >= keys[1].time) return keys[1].value;
                else {
                    float t = Mathf.InverseLerp(keys[0].time, keys[1].time, time);
                    return Quaternion.Lerp(keys[0].value, keys[1].value, t);
                }
            }
            else {

                int len = length;
                for (int i = 0; i < len; i++) {
                    if (keys[i].time > time) {

                        int p0Index = i;
                        if (i > 0) p0Index = i - 1;
                        else p0Index = i;

                        int p1Index = i;

                        Quaternion p0 = keys[p0Index].value;
                        Quaternion p1 = keys[p1Index].value;
                        //Debug.Log(keys.Length + " " + keys[0].time + " " + keys[1].time + " " + keys[2].time + " " + time);


                        float t = Mathf.InverseLerp(keys[p0Index].time, keys[p1Index].time, time);
                        return Quaternion.Slerp(p0,p1,t);
                    }
                }
                //Debug.Log("noneFound "+keys.Length + " " + keys[0].time + " " + keys[1].time + " " + keys[2].time + " " + time);
                return keys[keys.Length - 1].value;
            }
        }

        public void AddKey(float time, Quaternion value) {
            List<QuaternionKeyframe> qList = new List<QuaternionKeyframe>(keys);
            for (int i = 0; i < keys.Length; i++) {
                if (keys[i].time > time) {
                    qList.Insert(i, new QuaternionKeyframe(time, value));
                    keys = qList.ToArray();
                    return;
                }
            }
            qList.Add(new QuaternionKeyframe(time, value));
            keys = qList.ToArray();
        }

        /// <summary> Gets the rotation of the last key </summary>
        public Quaternion EvaluateEnd() {
            return keys[keys.Length - 1].value;
        }
    }

    [Serializable]
    public struct QuaternionKeyframe {
        public Quaternion value;
        public float time;

        public QuaternionKeyframe(float time, Quaternion value) {
            this.time = time;
            this.value = value;
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