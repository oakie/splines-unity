using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SplinePath))]
public class SplinePathInspector : Editor {
  public override void OnInspectorGUI() {
    DrawDefaultInspector();

    SplinePath spline = target as SplinePath;
    if (GUILayout.Button("Add Point")) {
      Undo.RecordObject(spline, "Add Point");
      spline.AddPoint();
    }
  }
}
