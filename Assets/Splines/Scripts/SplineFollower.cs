using UnityEngine;
using System.Collections;

public class SplineFollower : MonoBehaviour {

  public SplinePath path;

  public float Speed = 1f;

  public bool LookAhead;

  private float t = 0f;

  private float duration = 1f;

	private void Start () {}
	
	private void Update () {
    duration = path.Length / Speed;
    if (duration > 0) {
      t += Time.deltaTime / duration;
      if (t > 1f) {
        t -= 1f;
      }
      SplinePoint p = path.GetPoint(t);

      transform.position = p.Position;

      if (LookAhead) {
        transform.rotation = Quaternion.LookRotation(p.Tangent);
      }
    }
	}
}
