using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineTrain : MonoBehaviour {

    public bool distance = true;
    public enum TrainType { Clamp, Loop, PingPong }
    public Bezier3DSpline spline;
    public TrainType trainType;
    public float speed = 1;
    public float startPos = 0;

    [ContextMenu("TEST")]
    void Test( ) {
        for (int i = 0; i < 100000; i++) {
            spline.GetOrientationLocal(startPos);
        }
        for (int i = 0; i < 100000; i++) {
            spline.GetOrientationLocalFast(startPos);
        }
    }
    void Start() {
        if (!spline) Debug.LogWarning("Please assign a spline to SplineTrain", this);
    }

    void OnValidate() {
        //if (trainType == TrainType.Clamp) startPos = Mathf.Clamp(startPos, 0, spline.totalLength);
        if (spline != null) SetPos(startPos);
    }
	void Update () {
        if (!spline) return;
        SetPos((Time.time * speed) + startPos);
	}

    void SetPos(float pos) {
        switch (trainType) {
            case TrainType.Clamp:
                break;
            case TrainType.Loop:
                pos = Mathf.Repeat(pos, distance ? spline.totalLength : 1);
                break;
            case TrainType.PingPong:
                pos = Mathf.PingPong(pos, distance ? spline.totalLength : 1);
            break;
        }
        if (distance) {
            transform.position = spline.GetPoint(pos);
            transform.rotation = spline.GetOrientationFast(pos);
        }
        else {
            transform.position = spline.GetPoint(spline.DistanceToTime(pos));
            transform.rotation = spline.GetOrientationFast(spline.DistanceToTime(pos));
        }
    }
}
