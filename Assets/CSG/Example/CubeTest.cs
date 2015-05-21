using UnityEngine;
using System.Collections;
using ConstructiveSolidGeometry;

[RequireComponent(typeof(MeshFilter))]
public class CubeTest : MonoBehaviour {

	void Start () {

        var cube = CSG.cube(Vector3.zero, Vector3.one);
        GetComponent<MeshFilter>().mesh = cube.toMesh();

	}
}
