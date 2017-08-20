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
        if (trainType == TrainType.Clamp) startPos = Mathf.Clamp(startPos, 0, 1);
        SetPos(startPos);
    }
	void Update () {
        if (!spline) return;
        SetPos((Time.time * speed) + startPos);
	}

    void SetPos(float pos) {
        switch (trainType) {
        case TrainType.Clamp:
            transform.position = spline.GetPoint(pos);
            transform.rotation = Quaternion.LookRotation(spline.GetUp(pos));
            break;
        case TrainType.Loop:
            transform.position = spline.GetPoint(Mathf.Repeat(pos, 1));
            transform.rotation = Quaternion.LookRotation(spline.GetUp(Mathf.Repeat(pos, 1)));
            break;
            case TrainType.PingPong:
            transform.position = spline.GetPoint(Mathf.PingPong(pos, 1));
            transform.rotation = Quaternion.LookRotation(spline.GetUp(Mathf.PingPong(pos, 1)));
            break;
        }
    }
}
