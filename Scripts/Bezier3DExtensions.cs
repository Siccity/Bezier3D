using UnityEngine;

public static class Bezier3DExtensions {

	public static Quaternion CubicBezier(Quaternion a, Quaternion b, Quaternion c, Quaternion d, float t) {
        return Quaternion.Lerp(
            Quaternion.Lerp(
                Quaternion.Lerp(a, b, t),
                Quaternion.Lerp(b, c, t),
                t),
            Quaternion.Lerp(
                Quaternion.Lerp(b, c, t),
                Quaternion.Lerp(c, d, t),
                t),
            t);
    }
}
