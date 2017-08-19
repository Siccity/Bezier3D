using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineTrain : MonoBehaviour {

    public enum TrainType { Clamp, Loop, PingPong }
    public Bezier3DSpline spline;
    public TrainType trainType;
    public float speed = 1;
    public float startPos = 0;

    void Start() {
        if (!spline) Debug.LogWarning("Please assign a spline to SplineTrain", this);
    }

    void OnValidate() {
        SetPos(startPos);
    }
	void Update () {
        if (!spline) return;
        SetPos((Time.time * speed) + startPos);
	}

    void SetPos(float pos) {
        switch (trainType) {
        case TrainType.Clamp:
            transform.position = spline.GetPointByDistance(pos);
            break;
        case TrainType.Loop:
            transform.position = spline.GetPointByDistance(Mathf.Repeat(pos, spline.totalLength));
            break;
        case TrainType.PingPong:
            transform.position = spline.GetPointByDistance(Mathf.PingPong(pos, spline.totalLength));
            break;
        }
    }
}
