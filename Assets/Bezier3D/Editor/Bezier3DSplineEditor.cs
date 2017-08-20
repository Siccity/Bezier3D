using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CustomEditor(typeof(Bezier3DSpline))]
public class Bezier3DSplineEditor : Editor {
    public static bool mirror = true;
    public static float handleSize = 0.1f;
    public static Vector2 guiOffset = new Vector2(10, 10);
    public static bool visualizeTime;
    public static bool visualizeOrientation = true;
	int activeKnot = -1;
    List<int> selectedKnots = new List<int>();
    static Bezier3DSpline spline;

    [MenuItem("GameObject/BezierSpline",false,10)]
    static void CreateBezierSpline() {
        new GameObject("BezierSpline").AddComponent<Bezier3DSpline>();
    }

    void OnEnable() {
        //Code for changing component icon. Not working yet.
        /*var method = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method != null) {
            method.Invoke(null, new System.Object[] { AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Plugins/Circuit/DocumentComponentBehaviour.cs"), Resources.Load<Texture2D>("icon") });
            method.Invoke(null, new System.Object[] { AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Plugins/Circuit/DocumentComponentStateMachine.cs"), Resources.Load<Texture2D>("icon") });
        }*/
        spline = target as Bezier3DSpline;
    }

    void OnDisable() {
        Tools.hidden = false;
        SelectKnot(-1, false);
    }

    override public void OnInspectorGUI() {
		Bezier3DSpline spline = target as Bezier3DSpline;

        ValidateSelected();

        EditorGUILayout.LabelField("Spline settings");

        EditorGUI.indentLevel = 1;

        EditorGUI.BeginChangeCheck();
        int steps = spline.cacheDensity;
        steps = EditorGUILayout.DelayedIntField("Cache density", steps);
        if (EditorGUI.EndChangeCheck()) {
            spline.SetCacheDensity(steps);
        }

        EditorGUI.BeginChangeCheck();
        bool closed = spline.closed;
        closed = EditorGUILayout.Toggle(new GUIContent("Closed", "Generate an extra curve, connecting the final point to the first point."),closed);
        if (EditorGUI.EndChangeCheck()) {
            spline.SetClosed(closed);
            SceneView.RepaintAll();
        }
        EditorGUI.indentLevel = 0;

        EditorGUILayout.Space();
        if (activeKnot != -1) {
            EditorGUILayout.LabelField("Selected point ("+activeKnot+")");
            EditorGUI.indentLevel = 1;
            Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);

            EditorGUI.BeginChangeCheck();
            bool orientation = knot.orientation != null;
            orientation = EditorGUILayout.Toggle("Orientation", orientation);
            if (EditorGUI.EndChangeCheck()) {
                if (orientation) knot.orientation = Quaternion.identity;
                else knot.orientation = null;
                spline.SetKnot(activeKnot, knot);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            bool auto = knot.auto != 0f;
            auto = EditorGUILayout.Toggle("Auto", auto);
            if (EditorGUI.EndChangeCheck()) {
                if (auto) knot.auto = 0.5f;
                else knot.auto = 0f;
                spline.SetKnot(activeKnot, knot);
                SceneView.RepaintAll();
            }

            if (auto) {
                EditorGUI.BeginChangeCheck();
                knot.auto = EditorGUILayout.FloatField("Distance", knot.auto);
                if (EditorGUI.EndChangeCheck()) {
                    spline.SetKnot(activeKnot, knot);
                    SceneView.RepaintAll();
                }
            } else {
                EditorGUI.BeginChangeCheck();
                knot.position = EditorGUILayout.Vector3Field("Position", knot.position);
                if (EditorGUI.EndChangeCheck()) {
                    spline.SetKnot(activeKnot, knot);
                    SceneView.RepaintAll();
                }
                EditorGUI.BeginChangeCheck();
                knot.handleIn = EditorGUILayout.Vector3Field("Handle in", knot.handleIn);
                if (EditorGUI.EndChangeCheck()) {
                    if (mirror) knot.handleOut = -knot.handleIn;
                    spline.SetKnot(activeKnot, knot);
                    SceneView.RepaintAll();
                }
                EditorGUI.BeginChangeCheck();
                knot.handleOut = EditorGUILayout.Vector3Field("Handle out", knot.handleOut);
                if (EditorGUI.EndChangeCheck()) {
                    if (mirror) knot.handleIn = -knot.handleOut;
                    spline.SetKnot(activeKnot, knot);
                    SceneView.RepaintAll();
                }
            }
        }
	}

	void OnSceneGUI() {
        Handles.BeginGUI();
        Color defaultColor = GUI.contentColor;
        GUILayout.BeginArea(new Rect(guiOffset, new Vector2(100, 200)));
        GUIStyle style = (GUIStyle)"ChannelStripAttenuationMarkerSquare";
        GUI.contentColor = mirror? Color.green:Color.red;
        mirror = GUILayout.Toggle(mirror, new GUIContent("Handle Mirror", "Should opposite handles mirror edited handles?"), style);
        GUILayout.Space(4);
        GUI.contentColor = visualizeTime? Color.green:Color.red;
        visualizeTime = GUILayout.Toggle(visualizeTime, new GUIContent("Debug Time", "Visualize time along spline"), style);
        GUILayout.Space(4);
        GUI.contentColor = visualizeOrientation ? Color.green : Color.red;
        visualizeOrientation = GUILayout.Toggle(visualizeOrientation, new GUIContent("Debug Orientation", "Visualize orientation along spline"), style);
        GUILayout.EndArea();
        Handles.EndGUI();

        ValidateSelected();
        DrawUnselectedKnots();

        if (visualizeTime) VisualizeTime(10);
        if (visualizeOrientation) VisualizeOrientation();
        if (activeKnot != -1) {
            if (selectedKnots.Count == 1) {
                DrawSelectedSplitters();
                DrawSelectedKnot();
            } else {
                DrawMultiSelect();
            }
        }
    }

    void DrawMultiSelect() {
        Handles.color = Color.blue;
        for (int i = 0; i < selectedKnots.Count; i++) {
            if (Handles.Button(spline.transform.TransformPoint(spline.GetKnot(selectedKnots[i]).position), Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                SelectKnot(selectedKnots[i], true);
            }
        }
        Vector3 handlePos = Vector3.zero;
        if (Tools.pivotMode == PivotMode.Center) {
            for (int i = 0; i < selectedKnots.Count; i++) {
                handlePos += spline.GetKnot(selectedKnots[i]).position;
            }
            handlePos /= selectedKnots.Count;
        } else {
            handlePos = spline.GetKnot(activeKnot).position;
        }
        handlePos = spline.transform.TransformPoint(handlePos);

        Handles.PositionHandle(handlePos, Tools.handleRotation);
    }
    void DrawUnselectedKnots() {
        Handles.color = Color.white;
        for (int i = 0; i < spline.KnotCount; i++) {
            if (selectedKnots.Contains(i)) continue;
            Bezier3DSpline.Knot knot = spline.GetKnot(i);

            Vector3 knotWorldPos = spline.transform.TransformPoint(knot.position);
            if (Handles.Button(knotWorldPos, Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                SelectKnot(i, Event.current.control);
            }
        }
    }

    void DrawSelectedKnot() {
        Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);
        Handles.color = Color.green;

        Vector3 knotWorldPos = spline.transform.TransformPoint(knot.position);

        if (Tools.current == Tool.Move) {
            //Position handle
            EditorGUI.BeginChangeCheck();
            knotWorldPos = Handles.PositionHandle(knotWorldPos, Tools.handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Point");
                knot.position = spline.transform.InverseTransformPoint(knotWorldPos);
                spline.SetKnot(activeKnot, knot);
            }
        }
        else if (Tools.current == Tool.Rotate) {
            //Rotation handle
            EditorGUI.BeginChangeCheck();
            Quaternion rot = knot.orientation.HasValue ? knot.orientation.Value : Quaternion.identity;
            rot = Handles.RotationHandle(rot, knotWorldPos);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Point");
                knot.orientation = rot;
                spline.SetKnot(activeKnot, knot);
                Repaint();
            }
        }


        //In Handle
        if (knot.handleIn != Vector3.zero) {
            EditorGUI.BeginChangeCheck();
            Vector3 inHandleWorldPos = spline.transform.TransformPoint(knot.position + knot.handleIn);
            inHandleWorldPos = Handles.PositionHandle(inHandleWorldPos, Tools.handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Handle");
                knot.handleIn = spline.transform.InverseTransformPoint(inHandleWorldPos) - knot.position;
                if (mirror) knot.handleOut = -knot.handleIn;
                spline.SetKnot(activeKnot, knot);
            }
            Handles.DrawLine(knotWorldPos, inHandleWorldPos);
        }


        //outHandle
        if (knot.handleOut != Vector3.zero) {
            EditorGUI.BeginChangeCheck();
            Vector3 outHandleWorldPos = spline.transform.TransformPoint(knot.position + knot.handleOut);
            outHandleWorldPos = Handles.PositionHandle(outHandleWorldPos, Tools.handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Handle");
                knot.handleOut = spline.transform.InverseTransformPoint(outHandleWorldPos) - knot.position;
                if (mirror) knot.handleIn = -knot.handleOut;
                spline.SetKnot(activeKnot, knot);
            }
            Handles.DrawLine(knotWorldPos, outHandleWorldPos);
        }

        // Hotkeys
        Event e = Event.current;
        switch(e.type) {
        case EventType.KeyDown:
            if (e.keyCode == KeyCode.Delete) {
                if (spline.CurveCount > 1) {
                    Undo.RecordObject(spline,"Remove Bezier Point");
                    spline.RemoveKnot(activeKnot);
                    SelectKnot(-1, false);
                }
                e.Use();

            }
            if (e.keyCode == KeyCode.Escape) {
                SelectKnot(-1, false);
                e.Use();
            }
            break;
        } 
    }


    void DrawSelectedSplitters() {
        Handles.color = Color.blue;
        //Start add
        if (!spline.closed && activeKnot == 0) {
            Bezier3DCurve curve = spline.GetCurve(0);
            Vector3
                a = spline.transform.TransformPoint(curve.a),
                b = spline.transform.TransformDirection(curve.b.normalized);
            Handles.DrawDottedLine(a, a-b,3f);
            if (Handles.Button(a - b, Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                spline.InsertKnot(0, new Bezier3DSpline.Knot(curve.a - curve.b.normalized, Vector3.zero, curve.b.normalized * 0.5f));
            }
        }

        //End add
        if (!spline.closed && activeKnot == spline.CurveCount) {
            Bezier3DCurve curve = spline.GetCurve(spline.CurveCount-1);
            Vector3
                c = spline.transform.TransformDirection(curve.c.normalized),
                d = spline.transform.TransformPoint(curve.d);
            Handles.DrawDottedLine(d, d - c, 3f);
            if (Handles.Button(d - c, Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                spline.AddKnot(new Bezier3DSpline.Knot(curve.d - curve.c.normalized, curve.c.normalized * 0.5f, Vector3.zero));
                SelectKnot(spline.CurveCount, false);
            }
        }

        // Prev split
        if (spline.closed || activeKnot != 0) {

            Bezier3DCurve curve = spline.GetCurve(activeKnot == 0 ? spline.CurveCount-1 : activeKnot-1);
            Vector3 centerLocal = curve.GetPointDistance(curve.length * 0.5f);
            Vector3 center = spline.transform.TransformPoint(centerLocal);

            Vector3 a = curve.a + curve.b;
            Vector3 b = curve.c + curve.d;
            Vector3 ab = (b - a) * 0.3f;

            if (Handles.Button(center, Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                spline.InsertKnot(activeKnot == 0 ? spline.CurveCount : activeKnot, new Bezier3DSpline.Knot(centerLocal, -ab, ab));
                if (activeKnot == 0) SelectKnot(spline.CurveCount-1,false);
            }
        }

        // Next split
        if (activeKnot != spline.CurveCount) {
            Bezier3DCurve curve = spline.GetCurve(activeKnot);
            Vector3 centerLocal = curve.GetPointDistance(curve.length * 0.5f);
            Vector3 center = spline.transform.TransformPoint(centerLocal);

            Vector3 a = curve.a + curve.b;
            Vector3 b = curve.c + curve.d;
            Vector3 ab = (b - a) * 0.3f;

            if (Handles.Button(center, Camera.current.transform.rotation, handleSize, handleSize, Handles.CircleHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                spline.InsertKnot(activeKnot+1, new Bezier3DSpline.Knot(centerLocal, -ab, ab));
                SelectKnot(activeKnot + 1, false);
            }
        }

    }

    static void DrawSelectedHandle(Transform local, ref Vector3 a, ref Vector3 b) {
        //a
        Vector3 aWorldPos = local.TransformPoint(a);
        aWorldPos = Handles.PositionHandle(aWorldPos, Quaternion.identity);
        a = local.InverseTransformPoint(aWorldPos);

        //b
        Vector3 bWorldPos = local.TransformPoint(b);
        bWorldPos = Handles.PositionHandle(bWorldPos, Quaternion.identity);
        b = local.InverseTransformPoint(bWorldPos);

        //line
        Handles.DrawLine(aWorldPos, bWorldPos);
    }

    void ValidateSelected() {
        if (activeKnot > spline.CurveCount) SelectKnot(-1, false);
    }

    void SelectKnot(int i, bool add) {
        activeKnot = i;
        if (i == -1) {
            selectedKnots = new List<int>() { };
            Tools.hidden = false;
        }
        else { 
            Tools.hidden = true;
            if (add) {
                if (selectedKnots.Contains(i)) {
                    selectedKnots.Remove(i);
                    if (selectedKnots.Count == 0) {
                        activeKnot = -1;
                        Tools.hidden = false;
                    } else {
                        activeKnot = selectedKnots[selectedKnots.Count - 1];
                    }
                } else {
                    selectedKnots.Add(i);
                    activeKnot = i;
                }
            }
            else {
                selectedKnots = new List<int>() { i };
                activeKnot = i;
            }
        }
        Repaint();
    }

    private void VisualizeTime(int steps) {
        for (float t = 0f; t < 1f; t += 1f / (steps * spline.CurveCount)) {
            Vector3 point = spline.GetPoint(t);
            Handles.DrawLine(point, point + Vector3.up * 0.1f);
        }
    }

    private void VisualizeOrientation() {
        int steps = 30;
        /*for (float t = 0f; t < 1f; t += 1f / (steps * spline.CurveCount)) {
            Vector3 point = spline.GetPoint(t);
            Vector3 up = spline.GetUp(t);
            Handles.DrawLine(point, point + up * 0.1f);
        }*/
        for (float t = 0f; t < spline.totalLength; t += 0.1f) {
            Vector3 point = spline.GetPointByDistance(t);
            Vector3 up = spline.GetUp(spline.DistanceToTime(t));
            Handles.DrawLine(point, point + up * 0.1f);
        }
    }
}
