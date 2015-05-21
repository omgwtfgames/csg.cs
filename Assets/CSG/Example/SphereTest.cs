using UnityEngine;
using System.Collections;
using ConstructiveSolidGeometry;

[RequireComponent(typeof(MeshFilter))]
public class SphereTest : MonoBehaviour {

	void Start () {

        var sphere = CSG.sphere(Vector3.zero);
        GetComponent<MeshFilter>().mesh = sphere.toMesh();

	}
}
