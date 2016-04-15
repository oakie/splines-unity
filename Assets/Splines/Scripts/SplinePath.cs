using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SplinePath : MonoBehaviour {

  public GameObject Prefab;

  public bool ShowLine;

  public Material LineMaterial;

  public float LineWidth = 5f;

  [HideInInspector]
  public List<GameObject> Points = new List<GameObject>();

  private List<float> SplineLengths = new List<float>();

  private  List<float> AccSplineLengths = new List<float>();

  private int Precision = 20;

  public float Length {
    get {
      return AccSplineLengths.Count == 0 ? -1f :  AccSplineLengths[AccSplineLengths.Count - 1];
    }
  }

  public void AddPoint() {
    GameObject g = (GameObject) PrefabUtility.InstantiatePrefab(Prefab);
    g.transform.parent = transform;
    UpdatePoints();
  }

  public SplinePoint GetPoint(float t) {
    float meters = ParameterToMeters(t);
    int spline = GetSplineIndex(meters);

    float m = meters;
    if (spline > 0)
      m -= AccSplineLengths[spline - 1];

    return GetSplinePoint(spline, m);
  }

  private void Start() {
    UpdatePoints();
  }

  private void UpdatePoints() {
    Points.Clear();
    SplineLengths.Clear();
    AccSplineLengths.Clear();

    int k = 0;
    foreach (Transform child in transform) {
      child.name = "SplinePoint(" + k++ + ")";
      Points.Add(child.gameObject);
    }

    float total = 0f;
    for (int i = 0; i < Points.Count; ++i) {
      float length = CalculateSplineLength(i);
      total += length;
      SplineLengths.Add(length);
      AccSplineLengths.Add(total);

      LineRenderer line = Points[i].GetComponent<LineRenderer>();
      if(ShowLine) {
        SplinePoint[] points = GetSplinePoints(i);
        line.SetVertexCount(points.Length);
        line.sharedMaterial = LineMaterial;
        line.SetWidth(LineWidth, LineWidth);
        for(int j = 0; j < points.Length; ++j) {
          line.SetPosition(j, points[j].Position);
        }
      } else {
        line.SetVertexCount(0);
      }
    }
  }

  private int ClampSpline(int spline) {
    if (spline < 0) {
      return Points.Count - 1;
    } else if (spline > Points.Count) {
      return 1;
    } else if (spline > Points.Count - 1) {
      return 0;
    }
    return spline;
  }

  private float ParameterToMeters(float t) {
    return t * AccSplineLengths[AccSplineLengths.Count - 1];
  }

  private int GetSplineIndex(float meters) {
    for (int i = 0; i < AccSplineLengths.Count; ++i) {
      if (meters <= AccSplineLengths[i]) {
        return i;
      }
    }
    return AccSplineLengths.Count - 1;
  }

  private float CalculateSplineLength(int spline) {
    SplinePoint[] points = GetSplinePoints(spline);
    if (points.Length < 2) { return -1f; }

    float length = 0f;
    Vector3 lastPoint = points[0].Position;
    for (int i = 1; i < Precision; ++i) {
      Vector3 thisPoint = points[i].Position;
      length += (lastPoint - thisPoint).magnitude;
      lastPoint = thisPoint;
    }
    return length;
  }

  private SplinePoint GetSplinePoint(int spline, float meters) {
    SplinePoint[] points = GetSplinePoints(spline);
    if (points.Length < 2) { return null; }

    float length = 0f, l = 0f;
    SplinePoint lastPoint = points[0];
    for (int i = 1; i < Precision; ++i) {
      SplinePoint thisPoint = points[i];

      l = (lastPoint.Position - thisPoint.Position).magnitude;
      length += l;

      if(meters <= length) {
        float f = (meters - (length - l)) / l;
        Vector3 pos = Vector3.Lerp(lastPoint.Position, thisPoint.Position, f);
        Vector3 rot = Vector3.Slerp(lastPoint.Tangent, thisPoint.Tangent, f);
        return new SplinePoint { Position = pos, Tangent = rot };
      }
      lastPoint = thisPoint;
    }

    return lastPoint;
  }

  private SplinePoint[] GetSplinePoints(int spline) {
    if (Points.Count < 3) { return new SplinePoint[0]; }

    SplinePoint[] points = new SplinePoint[Precision];
    Vector3 p0 = Points[ClampSpline(spline - 1)].transform.position;
    Vector3 p1 = Points[spline].transform.position;
    Vector3 p2 = Points[ClampSpline(spline + 1)].transform.position;
    Vector3 p3 = Points[ClampSpline(spline + 2)].transform.position;

    float step = 1f / (Precision - 1);
    float t = 0;
    for (int i = 0; i < Precision; ++i) {
      points[i] = new SplinePoint { Position = GetSplinePoint(t, p0, p1, p2, p3), Tangent = GetSplineTangent(t, p0, p1, p2, p3) };
      t += step;
    }
    points[Precision - 1] = new SplinePoint { Position = GetSplinePoint(1f, p0, p1, p2, p3), Tangent = GetSplineTangent(1f, p0, p1, p2, p3) };
    return points;
  }

  private Vector3 GetSplinePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
    Vector3[] x = GetCatmullRomControlPoints(t, p0, p1, p2, p3);
    return x[0] + (x[1] * t) + (x[2] * t * t) + (x[3] * t * t * t);
  }

  private Vector3 GetSplineTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
    Vector3[] x = GetCatmullRomControlPoints(t, p0, p1, p2, p3);
    return x[1] + (2f * x[2] * t) + (3f * x[3] * t * t);
  }

  private Vector3[] GetCatmullRomControlPoints(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
    Vector3[] points = new Vector3[4];
    points[0] = 0.5f * (2f * p1);
    points[1] = 0.5f * (p2 - p0);
    points[2] = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
    points[3] = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);
    return points;
  }

#if UNITY_EDITOR
  private void OnEnable() {
    if (Application.isEditor) {
      EditorApplication.hierarchyWindowChanged += HierarchyChanged;
    }
  }

  private void OnDisable() {
    if (Application.isEditor) {
      EditorApplication.hierarchyWindowChanged -= HierarchyChanged;
    }
  }

  private void HierarchyChanged() {
    UpdatePoints();
  }

  private void OnDrawGizmos() {
    Gizmos.color = Color.magenta;
    for (int i = 0; i < Points.Count; i++) {
      if(!ShowLine) {
        DrawSpline(i);
      }
      DrawLabel("<" + i + ">", Points[i].transform.position);
    }
  }

  private void DrawSpline(int segment) {
    SplinePoint[] points = GetSplinePoints(segment);
    if (points.Length < 2) { return; }

    Vector3 lastPoint = points[0].Position;
    for (int i = 1; i < Precision; ++i) {
      Vector3 thisPoint = points[i].Position;
      Gizmos.DrawLine(lastPoint, thisPoint);
      lastPoint = thisPoint;
    }
  }

  private void DrawLabel(string text, Vector3 position) {
    GUIContent textContent = new GUIContent(text);
    GUIStyle style = new GUIStyle();
    style.normal.textColor = Color.white;
    style.fontSize = 16;
    Vector2 textSize = style.CalcSize(textContent);
    Vector3 screenPoint = Camera.current.WorldToScreenPoint(position);

    if (screenPoint.z > 0) {
      Vector3 worldPosition = Camera.current.ScreenToWorldPoint(new Vector3(screenPoint.x - textSize.x * 0.5f, screenPoint.y + textSize.y * 0.5f, screenPoint.z));
      Handles.Label(worldPosition, textContent, style);
    }
  }
#endif
}

public class SplinePoint {
  public Vector3 Position { get; set; }
  public Vector3 Tangent { get; set; }
}