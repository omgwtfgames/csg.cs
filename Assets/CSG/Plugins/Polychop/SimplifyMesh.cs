using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using Polychop;

public class SimplifyMesh : MonoBehaviour {
    public MeshFilter meshFilter;
    public int desiredVertices;

    void Awake() {
        Mesh m = meshFilter.mesh;
        Debug.Log("Original mesh: vertices, triangles = " + m.vertices.Length + ", " + m.triangles.Length/3);

        List<Vector> vertices = new List<Vector>();
        List<tridata> triangles = new List<tridata>();
        for (int vi=0; vi < m.vertices.Length; vi++) {
            Vector3 v = m.vertices[vi];
            vertices.Add(new Vector(v.x, v.y, v.z));
        }
        //Debug.Log("Extracted verticies: " + vertices.Count);

        int[] tris = m.triangles;
        for (int ti=0; ti < tris.Length; ti += 3) {
            triangles.Add(new tridata(tris[ti], tris[ti+1], tris[ti+2]));
        }
        //Debug.Log("Extracted triangles: " + triangles.Count);

        List<int> map = new List<int>();
        List<int> permutation = new List<int>();
        Polychop.Polychop chopper = new Polychop.Polychop();
        chopper.ProgressiveMesh(ref vertices, ref triangles, ref map, ref permutation);

        Debug.Log("Map:");
        for (int mi=0; mi < map.Count; mi++) {
            Debug.Log(mi.ToString() + ", " + map[mi].ToString());
        }

        Debug.Log("Permutation:");
        for (int pi=0; pi < permutation.Count; pi++) {
            Debug.Log(pi.ToString() + ", " + permutation[pi].ToString());
        }

        //UnityEngine.Debug.Log ("V, T = " + vertices.Count + "," + triangles.Count);
        PermuteVertices(permutation, ref vertices, ref triangles);
        //UnityEngine.Debug.Log ("V, T = " + vertices.Count + "," + triangles.Count);

        Mesh simpMesh = new Mesh();

        //List<Vector3> newNorms = new List<Vector3>();
        List<int> newTris = new List<int>();
        Vector3[] newVerts = new Vector3[desiredVertices];
        for (int ti=0; ti < triangles.Count; ti++) {
            int via = triangles[ti].v[0];
            int vib = triangles[ti].v[1];
            int vic = triangles[ti].v[2];
            int nvia = Map(via, desiredVertices, ref map);
            int nvib = Map(vib, desiredVertices, ref map);
            int nvic = Map(vic, desiredVertices, ref map);
            // only add vertices and make a triangle if all vertices are unique
            // (eg, no two or one sided triangles :) )
            if (nvia == nvib || nvia == nvic || nvib == nvic) continue;
            newTris.Add(nvia);
            newTris.Add(nvib);
            newTris.Add(nvic);

            //newVerts[nvia] = vectorToVector3(vertices[nvia]);
            //newVerts[nvib] = vectorToVector3(vertices[nvib]);
            //newVerts[nvic] = vectorToVector3(vertices[nvic]);

            //newVerts.Add(vectorToVector3(vertices[nvia]));
            //newVerts.Add(vectorToVector3(vertices[nvib]));
            //newVerts.Add(vectorToVector3(vertices[nvic]));

            /*
            Vector3 nrml = vectorToVector3((vertices[nvib]-vertices[nvia]) * (vertices[nvic]-vertices[nvib]));  // cross product
            nrml.Normalize();
            newNorms.Add(nrml);
            newNorms.Add(nrml);
            newNorms.Add(nrml);
            */
        }

        for (int vi=0; vi < desiredVertices; vi++) {
            newVerts[vi] = vectorToVector3(vertices[Map(vi, desiredVertices, ref map)]);
        }

        simpMesh.vertices = newVerts;
        //simpMesh.normals = newNorms.ToArray();
        simpMesh.triangles = newTris.ToArray();
        simpMesh.RecalculateNormals();
        meshFilter.mesh = simpMesh;
        Debug.Log("Simplified mesh: vertices, triangles = " + simpMesh.vertices.Length + ", " + simpMesh.triangles.Length/3);

        /*
        while (vertices.Count > desiredVertices) {
            Polychop.Polychop.Vertex mn = chopper.MinimumCostEdge();
            chopper.Collapse(mn, mn.collapse);
        }
        */
    }

    Vector3 vectorToVector3(Vector v) {
        return new UnityEngine.Vector3(v.x, v.y, v.z);
    }

    int Map(int a, int mx, ref List<int> map) {
        if (mx <= 0) return 0;
        while (a >= mx) {
            a = map[a];
        }
        return a;
    }

    void PermuteVertices(List<int> permutation, ref List<Vector> vertices, ref List<tridata> triangles) {
        // rearrange the vertex list
        List<Vector> temp_list = new List<Vector>();
        int i;
        //System.Diagnostics.Debug.Assert(permutation.Count == vertices.Count);
        for (i = 0; i < vertices.Count; i++) {
            temp_list.Add(vertices[i]);
        }
        for (i = 0; i < vertices.Count; i++) {
             vertices[permutation[i]] = temp_list[i];
        }
        // update the changes in the entries in the triangle list
        for (i = 0; i < triangles.Count; i++) {
            for (int j = 0; j < 3; j++) {
                triangles[i].v[j] = permutation[triangles[i].v[j]];
            }
        }
    }
}
