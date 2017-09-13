using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CustomEditor(typeof(Bezier3DSpline))]
public class Bezier3DSplineEditor : Editor {
    public static Action<Bezier3DSpline> onUpdateSpline;

    public static bool mirror = true;
    public static float handleSize = 0.1f;
    public static Vector2 guiOffset = new Vector2(10, 10);
    public static bool visualizeOrientation = true;
    int activeKnot = -1;
    List<int> selectedKnots = new List<int>();
    static Bezier3DSpline spline;

    [MenuItem("GameObject/BezierSpline", false, 10)]
    static void CreateBezierSpline() {
        new GameObject("BezierSpline").AddComponent<Bezier3DSpline>();
    }

    void OnEnable() {
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
            if (onUpdateSpline != null) onUpdateSpline(spline);
        }

        EditorGUI.BeginChangeCheck();
        bool closed = spline.closed;
        closed = EditorGUILayout.Toggle(new GUIContent("Closed", "Generate an extra curve, connecting the final point to the first point."), closed);
        if (EditorGUI.EndChangeCheck()) {
            spline.SetClosed(closed);
            if (onUpdateSpline != null) onUpdateSpline(spline);
            SceneView.RepaintAll();
        }

        Rect position = EditorGUILayout.GetControlRect(false, 19f, EditorStyles.numberField);
        position.xMin += EditorGUIUtility.labelWidth;

        Rect flipRect = new Rect(position.x, position.y, position.width, position.height);
        if (GUI.Button(flipRect, new GUIContent("Flip", "Flip spline direction."))) {
            spline.Flip();
            if (onUpdateSpline != null) onUpdateSpline(spline);
            SceneView.RepaintAll();
        }


        EditorGUI.indentLevel = 0;

        EditorGUILayout.Space();
        if (activeKnot != -1) {
            EditorGUILayout.LabelField("Selected point");
            EditorGUI.indentLevel = 1;
            Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);

            position = EditorGUILayout.GetControlRect(false, 19f, EditorStyles.numberField);
            position.xMin += EditorGUIUtility.labelWidth;

            EditorGUI.BeginChangeCheck();
            bool orientation = knot.orientation != null;
            Rect orientationRect = new Rect(position.x, position.y, position.height, position.height);
            orientation = GUI.Toggle(orientationRect, orientation, new GUIContent("O", "Orientation Anchor"), "Button");
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Toggle Bezier Orientation Anchor");
                if (orientation) knot.orientation = Quaternion.identity;
                else knot.orientation = null;
                spline.SetKnot(activeKnot, knot);
                if (onUpdateSpline != null) onUpdateSpline(spline);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            bool auto = knot.auto != 0f;
            Rect autoRect = new Rect(position.x + position.height + 4, position.y, position.height, position.height);
            auto = GUI.Toggle(autoRect, auto, new GUIContent("A", "Auto Handles"), "Button");
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Toggle Bezier Auto Handles");
                if (auto) knot.auto = 0.33f;
                else knot.auto = 0f;
                spline.SetKnot(activeKnot, knot);
                if (onUpdateSpline != null) onUpdateSpline(spline);
                SceneView.RepaintAll();
            }


            if (orientation) {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                Vector3 orientationEuler = knot.orientation.Value.eulerAngles;
                orientationEuler = EditorGUILayout.Vector3Field("Orientation", orientationEuler);
                if (EditorGUI.EndChangeCheck()) {
                    knot.orientation = Quaternion.Euler(orientationEuler);
                    spline.SetKnot(activeKnot, knot);
                    SceneView.RepaintAll();
                }
            }

            if (auto) {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                knot.position = EditorGUILayout.Vector3Field("Position", knot.position);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(spline, "Edit Bezier Point");
                    spline.SetKnot(activeKnot, knot);
                    if (onUpdateSpline != null) onUpdateSpline(spline);
                    SceneView.RepaintAll();
                }
                EditorGUI.BeginChangeCheck();
                knot.auto = EditorGUILayout.FloatField("Distance", knot.auto);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(spline, "Edit Bezier Point");
                    spline.SetKnot(activeKnot, knot);
                    if (onUpdateSpline != null) onUpdateSpline(spline);
                    SceneView.RepaintAll();
                }
            } else {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                knot.position = EditorGUILayout.Vector3Field("Position", knot.position);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(spline, "Edit Bezier Point");
                    spline.SetKnot(activeKnot, knot);
                    if (onUpdateSpline != null) onUpdateSpline(spline);
                    SceneView.RepaintAll();
                }
                EditorGUI.BeginChangeCheck();
                knot.handleIn = EditorGUILayout.Vector3Field("Handle in", knot.handleIn);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(spline, "Edit Bezier Handle");
                    if (mirror) knot.handleOut = -knot.handleIn;
                    spline.SetKnot(activeKnot, knot);
                    if (onUpdateSpline != null) onUpdateSpline(spline);
                    SceneView.RepaintAll();
                }
                EditorGUI.BeginChangeCheck();
                knot.handleOut = EditorGUILayout.Vector3Field("Handle out", knot.handleOut);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(spline, "Edit Bezier Handle");
                    if (mirror) knot.handleIn = -knot.handleOut;
                    spline.SetKnot(activeKnot, knot);
                    if (onUpdateSpline != null) onUpdateSpline(spline);
                    SceneView.RepaintAll();
                }
            }
        }
    }

    void Hotkeys() {
        Event e = Event.current;
        switch (e.type) {
            case EventType.ValidateCommand:
                if (e.commandName == "UndoRedoPerformed") if (onUpdateSpline != null) onUpdateSpline(spline);
                break;
            case EventType.KeyDown:
                if (e.keyCode == KeyCode.I) {
                    if ((e.modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0) {
                        spline.Flip();
                    }
                }
                break;
        }
    }

    void OnSceneGUI() {
        Hotkeys();

        Handles.BeginGUI();
        Color defaultColor = GUI.contentColor;
        GUILayout.BeginArea(new Rect(guiOffset, new Vector2(100, 200)));
        GUIStyle style = (GUIStyle)"ChannelStripAttenuationMarkerSquare";
        GUI.contentColor = mirror ? Color.green : Color.red;
        mirror = GUILayout.Toggle(mirror, new GUIContent("Handle Mirror", "Should opposite handles mirror edited handles?"), style);
        GUILayout.Space(4);
        GUI.contentColor = visualizeOrientation ? Color.green : Color.red;
        visualizeOrientation = GUILayout.Toggle(visualizeOrientation, new GUIContent("Show Orientation", "Visualize orientation along spline"), style);
        GUILayout.EndArea();
        Handles.EndGUI();

        ValidateSelected();
        DrawUnselectedKnots();

        if (visualizeOrientation) VisualizeOrientations();
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
        for (int i = 0; i < spline.KnotCount; i++) {
            if (selectedKnots.Contains(i)) continue;
            Bezier3DSpline.Knot knot = spline.GetKnot(i);

            Vector3 knotWorldPos = spline.transform.TransformPoint(knot.position);
            if (knot.orientation.HasValue) {
                Handles.color = Handles.yAxisColor;
                Quaternion rot = spline.transform.rotation * knot.orientation.Value;
                Handles.ArrowHandleCap(0, knotWorldPos, rot * Quaternion.AngleAxis(90, Vector3.left), 0.15f, EventType.repaint);
            }
            Handles.color = Color.white;
            if (Handles.Button(knotWorldPos, Camera.current.transform.rotation, HandleUtility.GetHandleSize(knotWorldPos) * handleSize, HandleUtility.GetHandleSize(knotWorldPos) * handleSize, Handles.CircleHandleCap)) {
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
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
        }
        else if (Tools.current == Tool.Rotate) {
            //Draw arrow

            //Rotation handle
            EditorGUI.BeginChangeCheck();
            Quaternion rot = knot.orientation.HasValue ? knot.orientation.Value : Quaternion.identity;
            Handles.color = Handles.yAxisColor;
            Handles.ArrowHandleCap(0, knotWorldPos, rot * Quaternion.AngleAxis(90, Vector3.left), HandleUtility.GetHandleSize(knotWorldPos), EventType.repaint);
            rot = Handles.RotationHandle(rot, knotWorldPos);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Point");
                knot.orientation = rot;
                spline.SetKnot(activeKnot, knot);
                if (onUpdateSpline != null) onUpdateSpline(spline);
                Repaint();
            }
        }


        Handles.color = Handles.zAxisColor;

        //In Handle
        if (knot.handleIn != Vector3.zero) {
            EditorGUI.BeginChangeCheck();
            Vector3 inHandleWorldPos = spline.transform.TransformPoint(knot.position + knot.handleIn);
            //inHandleWorldPos = Handles.PositionHandle(inHandleWorldPos, Tools.handleRotation);
            if (knot.auto == 0) inHandleWorldPos = SmallPositionHandle(inHandleWorldPos, Tools.handleRotation,0.5f,1f);
            else inHandleWorldPos = SmallPositionHandle(inHandleWorldPos, Tools.handleRotation,0.5f,0.5f);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Handle");
                knot.handleIn = spline.transform.InverseTransformPoint(inHandleWorldPos) - knot.position;
                knot.auto = 0;
                if (mirror) knot.handleOut = -knot.handleIn;
                spline.SetKnot(activeKnot, knot);
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
            Handles.DrawLine(knotWorldPos, inHandleWorldPos);
        }


        //outHandle
        if (knot.handleOut != Vector3.zero) {
            EditorGUI.BeginChangeCheck();
            Vector3 outHandleWorldPos = spline.transform.TransformPoint(knot.position + knot.handleOut);
            //outHandleWorldPos = Handles.PositionHandle(outHandleWorldPos, Tools.handleRotation);
            if (knot.auto == 0) outHandleWorldPos = SmallPositionHandle(outHandleWorldPos, Tools.handleRotation,0.5f, 1f);
            else outHandleWorldPos = SmallPositionHandle(outHandleWorldPos, Tools.handleRotation,0.5f, 0.5f);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edit Bezier Handle");
                knot.handleOut = spline.transform.InverseTransformPoint(outHandleWorldPos) - knot.position;
                knot.auto = 0;
                if (mirror) knot.handleIn = -knot.handleOut;
                spline.SetKnot(activeKnot, knot);
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
            Handles.DrawLine(knotWorldPos, outHandleWorldPos);
        }

        // Hotkeys
        Event e = Event.current;
        switch (e.type) {
            case EventType.KeyDown:
                if (e.keyCode == KeyCode.Delete) {
                    if (spline.KnotCount > 2) {
                        Undo.RecordObject(spline, "Remove Bezier Point");
                        spline.RemoveKnot(activeKnot);
                        SelectKnot(-1, false);
                        if (onUpdateSpline != null) onUpdateSpline(spline);
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
        Handles.color = Color.white;
        //Start add
        if (!spline.closed && activeKnot == 0) {
            Bezier3DCurve curve = spline.GetCurve(0);
            Vector3
                a = spline.transform.TransformPoint(curve.a),
                b = spline.transform.TransformDirection(curve.b.normalized) * 2f;

            float handleScale = HandleUtility.GetHandleSize(a);
            b *= handleScale;
            Handles.DrawDottedLine(a, a - b, 3f);
            if (Handles.Button(a - b, Camera.current.transform.rotation, handleScale * handleSize * 0.4f, handleScale * handleSize * 0.4f, Handles.DotHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);
                spline.InsertKnot(0, new Bezier3DSpline.Knot(curve.a - (curve.b.normalized * handleScale * 2), Vector3.zero, curve.b.normalized * 0.5f,knot.auto, knot.orientation));
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
        }

        //End add
        if (!spline.closed && activeKnot == spline.CurveCount) {
            Bezier3DCurve curve = spline.GetCurve(spline.CurveCount - 1);
            Vector3
                c = spline.transform.TransformDirection(curve.c.normalized) * 2f,
                d = spline.transform.TransformPoint(curve.d);
            float handleScale = HandleUtility.GetHandleSize(d);
            c *= handleScale;
            Handles.DrawDottedLine(d, d - c, 3f);
            if (Handles.Button(d - c, Camera.current.transform.rotation, handleScale * handleSize * 0.4f, handleScale * handleSize * 0.4f, Handles.DotHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);
                spline.AddKnot(new Bezier3DSpline.Knot(curve.d - (curve.c.normalized * handleScale * 2), curve.c.normalized * 0.5f, Vector3.zero, knot.auto, knot.orientation));
                SelectKnot(spline.CurveCount, false);
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
        }

        // Prev split
        if (spline.closed || activeKnot != 0) {

            Bezier3DCurve curve = spline.GetCurve(activeKnot == 0 ? spline.CurveCount - 1 : activeKnot - 1);
            Vector3 centerLocal = curve.GetPoint(curve.Dist2Time(curve.length * 0.5f));
            Vector3 center = spline.transform.TransformPoint(centerLocal);

            Vector3 a = curve.a + curve.b;
            Vector3 b = curve.c + curve.d;
            Vector3 ab = (b - a) * 0.3f;
            float handleScale = HandleUtility.GetHandleSize(center);

            if (Handles.Button(center, Camera.current.transform.rotation, handleScale * handleSize * 0.4f, handleScale * handleSize * 0.4f, Handles.DotHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                Bezier3DSpline.Knot knot = spline.GetKnot(activeKnot);
                spline.InsertKnot(activeKnot == 0 ? spline.CurveCount : activeKnot, new Bezier3DSpline.Knot(centerLocal, -ab, ab, knot.auto, knot.orientation));
                if (activeKnot == 0) SelectKnot(spline.CurveCount - 1, false);
                if (onUpdateSpline != null) onUpdateSpline(spline);
            }
        }

        // Next split
        if (activeKnot != spline.CurveCount) {
            Bezier3DCurve curve = spline.GetCurve(activeKnot);
            Vector3 centerLocal = curve.GetPoint(curve.Dist2Time(curve.length * 0.5f));
            Vector3 center = spline.transform.TransformPoint(centerLocal);

            Vector3 a = curve.a + curve.b;
            Vector3 b = curve.c + curve.d;
            Vector3 ab = (b - a) * 0.3f;
            float handleScale = HandleUtility.GetHandleSize(center);
            if (Handles.Button(center, Camera.current.transform.rotation, handleScale * handleSize * 0.4f, handleScale * handleSize * 0.4f, Handles.DotHandleCap)) {
                Undo.RecordObject(spline, "Add Bezier Point");
                spline.InsertKnot(activeKnot + 1, new Bezier3DSpline.Knot(centerLocal, -ab, ab));
                SelectKnot(activeKnot + 1, false);
                if (onUpdateSpline != null) onUpdateSpline(spline);
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

    private void VisualizeOrientations() {
        for (float dist = 0f; dist < spline.totalLength; dist += 1) {
            Vector3 point = spline.GetPoint(dist);
            Quaternion rot = spline.GetOrientationFast(dist);

            Vector3 up = rot * Vector3.up;
            Handles.color = Color.white;
            Handles.DrawLine(point, point + up);

            Vector3 forward = rot * Vector3.forward;
            Handles.color = Handles.zAxisColor;
            Handles.DrawLine(point, point + forward);
        }
    }

    private Vector3 SmallPositionHandle(Vector3 position, Quaternion rotation, float size, float alpha) {
        float handleSize = HandleUtility.GetHandleSize(position) * size;
        Color color = Handles.color;

        Color col;
        //X axis
        col = Handles.xAxisColor;
        col.a = alpha;
        Handles.color = col;
        GUI.SetNextControlName("xAxis");
        position = Handles.Slider(position, rotation * Vector3.right, handleSize, Handles.ArrowHandleCap, EditorPrefs.GetFloat("MoveSnapX"));

        //Y axis
        col = Handles.yAxisColor;
        col.a = alpha;
        Handles.color = col;
        GUI.SetNextControlName("yAxis");
        position = Handles.Slider(position, rotation * Vector3.up, handleSize, Handles.ArrowHandleCap, EditorPrefs.GetFloat("MoveSnapY"));

        //Z axis
        col = Handles.zAxisColor;
        col.a = alpha;
        Handles.color = col;
        GUI.SetNextControlName("zAxis");
        position = Handles.Slider(position, rotation * Vector3.forward, handleSize, Handles.ArrowHandleCap, EditorPrefs.GetFloat("MoveSnapZ"));
        //Handles.Slider2D()
        /*
        if (Handles.free) {
            Handles.color = Handles.centerColor;
            GUI.SetNextControlName("FreeMoveAxis");
            Vector3 arg_1CF_0 = position;
            float arg_1CF_2 = handleSize * 0.15f;
            Vector3 arg_1CF_3 = SnapSettings.move;
            if (Handles.<> f__mg$cache5 == null)
				{
                Handles.<> f__mg$cache5 = new Handles.CapFunction(Handles.RectangleHandleCap);
            }
            position = Handles.FreeMoveHandle(arg_1CF_0, rotation, arg_1CF_2, arg_1CF_3, Handles.<> f__mg$cache5);
        }*/
        position = DoPlanarHandle(PlaneHandle.xyPlane, position, rotation, HandleUtility.GetHandleSize(position) * 0.2f, alpha);
        position = DoPlanarHandle(PlaneHandle.xzPlane, position, rotation, HandleUtility.GetHandleSize(position) * 0.2f, alpha);
        position = DoPlanarHandle(PlaneHandle.yzPlane, position, rotation, HandleUtility.GetHandleSize(position) * 0.2f, alpha);

        Handles.color = color;
        return position;
    }

    private enum PlaneHandle {
        xzPlane,
        xyPlane,
        yzPlane
    }

    private static Vector3 DoPlanarHandle(PlaneHandle planeID, Vector3 handlePos, Quaternion rotation, float handleSize, float alpha) {
        int num = 0;
        int num2 = 0;
        switch (planeID) {
            case PlaneHandle.xyPlane:
                Handles.color = new Color(Handles.zAxisColor.r, Handles.zAxisColor.g, Handles.zAxisColor.b, alpha);
                num = 0;
                num2 = 1;
                break;
            case PlaneHandle.xzPlane:
                Handles.color = new Color(Handles.yAxisColor.r, Handles.yAxisColor.g, Handles.yAxisColor.b, alpha);
                num = 0;
                num2 = 2;
                break;
            case PlaneHandle.yzPlane:
                Handles.color = new Color(Handles.xAxisColor.r, Handles.xAxisColor.g, Handles.xAxisColor.b, alpha);
                num = 1;
                num2 = 2;
                break;
        }
        int index = 3 - num2 - num;
        Color color = Handles.color;

        Matrix4x4 matrix4x = Matrix4x4.TRS(handlePos, rotation, Vector3.one);
        Vector3 normalized;
        if (Camera.current.orthographic) {
            normalized = matrix4x.inverse.MultiplyVector(SceneView.currentDrawingSceneView.rotation * -Vector3.forward).normalized;
        }
        else {
            normalized = matrix4x.inverse.MultiplyPoint(SceneView.currentDrawingSceneView.camera.transform.position).normalized;
        }

        Vector3 result = handlePos;
        if (Mathf.Abs(normalized[index]) < 0.05f) {
            Handles.color = color;
            result = handlePos;
        }
        else {
            int id = GUIUtility.GetControlID(planeID.GetHashCode(), FocusType.Passive);
            Vector3 offset = Vector3.one;
            offset[num] = (normalized[num] >= -0.01f) ? 1 : -1;
            offset[num2] = (normalized[num2] >= -0.01f) ? 1 : -1;
            offset[index] = 0f;
            offset = rotation * (offset * handleSize * 0.5f);
            Vector3 slideDir1 = Vector3.zero;
            Vector3 slideDir2 = Vector3.zero;
            Vector3 handleDir = Vector3.zero;
            slideDir1[num] = 1f;
            slideDir2[num2] = 1f;
            handleDir[index] = 1f;
            slideDir1 = rotation * slideDir1;
            slideDir2 = rotation * slideDir2;
            handleDir = rotation * handleDir;
            Vector3[] verts = new Vector3[4] {
                handlePos + offset + (slideDir1 + slideDir2) * handleSize * 0.5f,
                handlePos + offset + (-slideDir1 + slideDir2) * handleSize * 0.5f,
                handlePos + offset + (-slideDir1 - slideDir2) * handleSize * 0.5f,
                handlePos + offset + (slideDir1 - slideDir2) * handleSize * 0.5f
            };
            Vector3 snapSettings = new Vector3(EditorPrefs.GetFloat("MoveSnapX"), EditorPrefs.GetFloat("MoveSnapY"), EditorPrefs.GetFloat("MoveSnapZ"));
            Handles.DrawSolidRectangleWithOutline(verts, new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.1f), new Color(0f, 0f, 0f, 0f));
            handlePos = Handles.Slider2D(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize * 0.5f, Handles.RectangleHandleCap, new Vector2(snapSettings[num], snapSettings[num2]));
            Handles.color = color;
            result = handlePos;
        }
        return result;
    }
}
