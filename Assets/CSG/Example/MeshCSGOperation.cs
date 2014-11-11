using UnityEngine;
using System.Collections;
using ConstructiveSolidGeometry;

public class MeshCSGOperation : MonoBehaviour
{

    /*
     *  Apply a CSG operation to the meshes for specified GameObjects a and b.
     *  If a and b are not specified (null), grab the meshes from the first to children of the transform.
     *  newObjectPrefab is cloned and given the resulting mesh after the CSG operation.
     */

    public enum Operation { Subtract, Union, Intersection };
    public Operation operation;
    public GameObject a;
    public GameObject b;
    public GameObject newObjectPrefab;

    void Start()
    {

        Transform[] childs = new Transform[2];
        if (a == null && b == null)
        {
            int i = 0;
            foreach (Transform t in transform)
            {
                if (i > 2) break;
                childs[i] = t;
                i++;
            }
        }
        else
        {
            childs[0] = a.transform;
            childs[1] = b.transform;
        }
        CSG A = CSG.fromMesh(childs[0].GetComponent<MeshFilter>().mesh, childs[0]);
        CSG B = CSG.fromMesh(childs[1].GetComponent<MeshFilter>().mesh, childs[1]);

        CSG result = null;
        if (operation == Operation.Subtract)
        {
            result = A.subtract(B);
        }
        if (operation == Operation.Union)
        {
            result = A.union(B);
        }
        if (operation == Operation.Intersection)
        {
            result = A.intersect(B);
        }

        /*
         * Debug.Log(A.polygons.Count + ", " + B.polygons.Count + ", " + result.polygons.Count);
        foreach (Polygon p in result.polygons) {
            Debug.Log("Result: " + p.vertices[0].pos+", "+p.vertices[1].pos+", "+p.vertices[2].pos);
            if (p.vertices.Length > 3) Debug.Log("!!! " + p.vertices.Length);
        }
        */

        GameObject newGo = Instantiate(newObjectPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        if (result != null) newGo.GetComponent<MeshFilter>().mesh = result.toMesh();
        childs[0].gameObject.SetActive(false);
        childs[1].gameObject.SetActive(false);
    }
}
