using UnityEngine;
using System.Collections;

public static class VectorExtensionMethods {

	public static Vector2 xy(this Vector3 v) {
		return new Vector2(v.x, v.y);
	}

	public static Vector3 WithX(this Vector3 v, float x) {
		return new Vector3(x, v.y, v.z);
	}

	public static Vector3 WithY(this Vector3 v, float y) {
		return new Vector3(v.x, y, v.z);
	}

	public static Vector3 WithZ(this Vector3 v, float z) {
		return new Vector3(v.x, v.y, z);
	}

	public static Vector2 WithX(this Vector2 v, float x) {
		return new Vector2(x, v.y);
	}
	
	public static Vector2 WithY(this Vector2 v, float y) {
		return new Vector2(v.x, y);
	}
	
	public static Vector3 WithZ(this Vector2 v, float z) {
		return new Vector3(v.x, v.y, z);
    }

    public static bool ApproximatelyEqual(this Vector3 a, Vector3 b)
    {
        if (Mathf.Approximately(a.x, b.x) && 
            Mathf.Approximately(a.y, b.y) &&
            Mathf.Approximately(a.z, b.z))
        {
            return true;
        }
        return false;
    }

    public static bool ApproximatelyEqual(this Vector2 a, Vector2 b)
    {
        if (Mathf.Approximately(a.x, b.x) &&
            Mathf.Approximately(a.y, b.y))
        {
            return true;
        }
        return false;
    }
}
