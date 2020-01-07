using UnityEditor;
using UnityEngine;

[CustomPreview(typeof(Bezier3DSpline))]
public class Bezier3DSplinePreview : ObjectPreview {

    private class Styles {
        public GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        public GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);

        public Styles() {
            Color textColor = new Color(0.7f, 0.7f, 0.7f);
            labelStyle.padding.right += 4;
            labelStyle.normal.textColor = textColor;
            headerStyle.padding.right += 4;
            headerStyle.normal.textColor = textColor;
        }
    }

    private Styles styles = new Styles();

    public override bool HasPreviewGUI() {
        return true;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background) {
        if (Event.current.type == EventType.Repaint) {
            Bezier3DSpline spline = target as Bezier3DSpline;

            RectOffset rectOffset = new RectOffset(-5, -5, -5, -5);
            r = rectOffset.Add(r);
            Rect position1 = r;
            Rect position2 = r;
            position1.width = 110f;
            position2.xMin += 110f;
            position2.width = 110f;
            EditorGUI.LabelField(position1, "Property", styles.headerStyle);
            EditorGUI.LabelField(position2, "Value", styles.headerStyle);
            position1.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position2.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            ShowProperty(ref position1, ref position2, "Point Count", spline.KnotCount.ToString());
            ShowProperty(ref position1, ref position2, "Total Length", spline.totalLength.ToString());
        }
    }

    private void ShowProperty(ref Rect labelRect, ref Rect valueRect, string label, string value) {
        EditorGUI.LabelField(labelRect, label, styles.labelStyle);
        EditorGUI.LabelField(valueRect, value, styles.labelStyle);
        labelRect.y += EditorGUIUtility.singleLineHeight;
        valueRect.y += EditorGUIUtility.singleLineHeight;
    }
}