/*
 *    C# port of polychop by Andrew Perry, 2013. Provided under the MIT license.
 *    Based on the original Polygon Reduction Demo by Stan Melax (c) 1998 (http://www.melax.com/polychop)
 *
 *  The function ProgressiveMesh() takes a model in an "indexed face
 *  set" sort of way.  i.e. list of vertices and list of triangles.
 *  The function then does the polygon reduction algorithm
 *  internally and reduces the model all the way down to 0
 *  vertices and then returns the order in which the
 *  vertices are collapsed and to which neighbor each vertex
 *  is collapsed to.  More specifically the returned "permutation"
 *  indicates how to reorder your vertices so you can render
 *  an object by using the first n vertices (for the n
 *  vertex version).  After permuting your vertices, the
 *  map list indicates to which vertex each vertex is collapsed to.
 */

namespace Polychop {
using UnityEngine; // HACK: temporary for Debug messages - otherwise remove so we can use threading.
using System;
using System.Diagnostics;
using System.Collections.Generic;

public class Polychop {

    List<Vertex> vertices; // = new List<Vertex>();
    List<Triangle> triangles; // = new List<Triangle>();

    /*
    public Polychop(ref List<Vector> _verts, ref List<tridata> _tris) {
        vertices = new List<Vertex>();
        triangles = new List<Triangle>();
        AddVertex(_verts);
        AddFaces(_tris);
    }

    */

    Polychop(List<Vertex> _vertices, List<Triangle> _triangles) {
        vertices = _vertices;
        triangles = _triangles;
    }

    public Polychop() : this (new List<Vertex>(), new List<Triangle>()) { }


    public void PermuteVertices(List<Vector> vert, List<tridata> tri, List<int> permutation) {
        // rearrange the vertex list
        List<Vector> temp_list = new List<Vector>();
        int i;
        //System.Diagnostics.Debug.Assert(permutation.Count == vert.Count);
        for (i = 0; i < vert.Count; i++) {
            temp_list.Add(vert[i]);
        }
        for (i = 0; i < vert.Count; i++) {
            vert[permutation[i]] = temp_list[i];
        }
        // update the changes in the entries in the triangle list
        for (i = 0; i < tri.Count; i++) {
            for (int j = 0;j < 3;j++) {
                tri[i].v[j] = permutation[tri[i].v[j]];
            }
        }
    }

    /*
    public static class GlobalMembers
    {
        public static List<Vertex > vertices = new List<Vertex>();
        public static List<Triangle > triangles = new List<Triangle>();
    }
    */

    public class Triangle {
        public Vertex[] vertex = new Vertex[3]; // the 3 points that make this tri
        public Vector normal = new Vector(); // unit vector othogonal to this face

        public Triangle(Vertex v0, Vertex v1, Vertex v2) {
            //System.Diagnostics.Debug.Assert(v0 != v1 && v1 != v2 && v2 != v0);
            vertex[0] = v0;
            vertex[1] = v1;
            vertex[2] = v2;
            ComputeNormal();
            // we don't add ourself to the triangle list, must be done by
            // whoever calls new Triangle
            //triangles.Add(this);
            for (int i = 0;i < 3;i++)
            {
                vertex[i].face.Add(this);
                for (int j = 0;j < 3;j++)
                {
                    if (i != j)
                    {
                    // TODO: optimization - would this be faster if Vertex.neighbor was a HashSet, so
                    //                      that added items are always unique ?
                    if (!vertex[i].neighbor.Contains(vertex[j])) vertex[i].neighbor.Add(vertex[j]);
                    }
                }
            }
        }

        public void Dispose() {
            int i;
            for (i = 0; i < 3; i++) {
                if (vertex[i] != null) {
                    vertex[i].face.Remove(this);
                }
            }
            for (i = 0; i < 3; i++) {
                int i2 = (i + 1) % 3;
                if (vertex[i] == null || vertex[i2] == null) continue;
                vertex[i].RemoveIfNonNeighbor(vertex[i2]);
                vertex[i2].RemoveIfNonNeighbor(vertex[i]);
            }
        }

        public bool HasVertex(Vertex v) {
            return (v == vertex[0] || v == vertex[1] || v == vertex[2]);
        }

        public void ComputeNormal() {
            Vector v0 = vertex[0].position;
            Vector v1 = vertex[1].position;
            Vector v2 = vertex[2].position;
            normal = (v1 - v0) * (v2 - v1);
            if (Vector.magnitude(normal) == 0)
                return;
            normal = Vector.normalize(normal);
        }

        public void ReplaceVertex(Vertex vold, Vertex vnew) {
            //System.Diagnostics.Debug.Assert(vold != null && vnew != null);
            //System.Diagnostics.Debug.Assert(vold == vertex[0] || vold == vertex[1] || vold == vertex[2]);
            //System.Diagnostics.Debug.Assert(vnew != vertex[0] && vnew != vertex[1] && vnew != vertex[2]);
            if (vold == vertex[0])
            {
                vertex[0] = vnew;
            }
            else if (vold == vertex[1])
            {
                vertex[1] = vnew;
            }
            else
            {
                //System.Diagnostics.Debug.Assert(vold == vertex[2]);
                vertex[2] = vnew;
            }
            int i;
            vold.face.Remove(this);
            //System.Diagnostics.Debug.Assert(!vnew.face.Contains(this));
            vnew.face.Add(this);
            for (i = 0;i < 3;i++)
            {
                vold.RemoveIfNonNeighbor(vertex[i]);
                vertex[i].RemoveIfNonNeighbor(vold);
            }
            for (i = 0;i < 3;i++)
            {
                //System.Diagnostics.Debug.Assert(vertex[i].face.Contains(this));
                for (int j = 0;j < 3;j++)
                {
                    if (i != j)
                    {
                    // TODO: optimization - use a HashSet for Vertex.neighbor so items are always unique ?
                    if (!vertex[i].neighbor.Contains(vertex[j])) vertex[i].neighbor.Add(vertex[j]);
                    }
                }
            }
            ComputeNormal();
        }
    }

    public class Vertex {
        public Vector position = new Vector(); // location of point in euclidean space
        public int id; // place of vertex in original list
        public List<Vertex> neighbor = new List<Vertex>(); // adjacent vertices
        public List<Triangle> face = new List<Triangle>(); // adjacent triangles
        public float objdist; // cached cost of collapsing edge
        public Vertex collapse; // candidate vertex for collapse

        public Vertex(Vector v, int _id) {
            position = v;
            id = _id;
            // caller of new Vertex must add the new object to verticies list themselves
            //vertices.Add(this);
        }

        public void Dispose() {
            //System.Diagnostics.Debug.Assert(face.Count == 0);
            while (neighbor.Count > 0) {
                neighbor[0].neighbor.Remove(this);
                neighbor.Remove(neighbor[0]);
            }
        }

        public void RemoveIfNonNeighbor(Vertex n) {
            // removes n from neighbor list if n isn't a neighbor.
            if (!neighbor.Contains(n))
                return;
            for (int i = 0;i < face.Count;i++)
            {
                if (face[i].HasVertex(n))
                    return;
            }
            neighbor.Remove(n);
        }
    }

    float ComputeEdgeCollapseCost(Vertex u, Vertex v) {
        // if we collapse edge uv by moving u to v then how 
        // much different will the model change, i.e. how much "error".
        // Texture, vertex normal, and border vertex code was removed
        // to keep this demo as simple as possible.
        // The method of determining cost was designed in order 
        // to exploit small and coplanar regions for
        // effective polygon reduction.
        // Is is possible to add some checks here to see if "folds"
        // would be generated.  i.e. normal of a remaining face gets
        // flipped.  I never seemed to run into this problem and
        // therefore never added code to detect this case.
        int i;
        float edgelength = Vector.magnitude(v.position - u.position);
        float curvature = 0F;
    
        // find the "sides" triangles that are on the edge uv
        List<Triangle> sides = new List<Triangle>();
        for (i = 0; i < u.face.Count; i++) {
            if (u.face[i].HasVertex(v)) {
                sides.Add(u.face[i]);
            }
        }
        // use the triangle facing most away from the sides
        // to determine our curvature term
        for (i = 0; i < u.face.Count; i++) {
            float mincurv = 1F; // curve for face i and closer side to it
            for (int j = 0; j < sides.Count; j++) {
                // use dot product of face normals. '^' operator defined in Vector
                float dotprod = u.face[i].normal ^ sides[j].normal;
                mincurv = (float) Math.Min((double) mincurv, (double) (1 - dotprod) / 2.0f);
            }
            curvature = (float) Math.Max((double)curvature, (double)mincurv);
        }
        // the more coplanar the lower the curvature term
        return edgelength * curvature;
    }
    
    void ComputeEdgeCostAtVertex(Vertex v) {
        // compute the edge collapse cost for all edges that start
        // from vertex v.  Since we are only interested in reducing
        // the object by selecting the min cost edge at each step, we
        // only cache the cost of the least cost edge at this vertex
        // (in member variable collapse) as well as the value of the 
        // cost (in member variable objdist).
        if (v.neighbor.Count == 0) {
            // v doesn't have neighbors so it costs nothing to collapse
            v.collapse = null;
            v.objdist = -0.01f;
            return;
        }
        v.objdist = 1000000f;
        v.collapse = null;
        // search all neighboring edges for "least cost" edge
        for (int i = 0; i < v.neighbor.Count; i++) {
            float dist;
            dist = ComputeEdgeCollapseCost(v, v.neighbor[i]);
            if (dist < v.objdist) {
                v.collapse = v.neighbor[i]; // candidate for edge collapse
                v.objdist = dist; // cost of the collapse
            }
        }
    }
    
    void ComputeAllEdgeCollapseCosts() {
        // For all the edges, compute the difference it would make
        // to the model if it was collapsed.  The least of these
        // per vertex is cached in each vertex object.
        for (int i = 0; i < vertices.Count; i++) {
            ComputeEdgeCostAtVertex(vertices[i]);
        }
    }
    
    public void Collapse(Vertex u, Vertex v) {
        // Collapse the edge uv by moving vertex u onto v
        // Actually remove tris on uv, then update tris that
        // have u to have v, and then remove u.
        if (v == null) {
            // u is a vertex all by itself so just delete it

            // replaces C++ delete
            u.Dispose();
            vertices.Remove(u);
            u = null;

            //UnityEngine.Debug.Log("Collapse: null vector, skipping");
            return;
        }
        int i;
        List<Vertex> tmp = new List<Vertex>();
        // make tmp a list of all the neighbors of u
        // TODO: maybe this could be optimized with AddRange instead
        for (i = 0; i < u.neighbor.Count; i++) {
            tmp.Add(u.neighbor[i]);
            //UnityEngine.Debug.Log("Collapse: create tmp neighbors");
        }
        // delete triangles on edge uv
        for (i = u.face.Count - 1; i >= 0; i--) {
            if (u.face[i].HasVertex(v)) {

                //u.face.RemoveAt(i);
                // replaces C++ delete
                triangles.Remove(u.face[i]);
                u.face[i].Dispose();
                //u.face[i] = null;

                //UnityEngine.Debug.Log("Collapse: delete triangles on edge uv");
            }
        }
        // update remaining triangles to have v instead of u
        for (i = u.face.Count - 1;i >= 0;i--) {
            u.face[i].ReplaceVertex(u,v);
            //UnityEngine.Debug.Log("Collapse: update remaining triangles to have v instead of u edge");
        }
        // replaces C++ delete
        vertices.Remove(u);
        u.Dispose();
        u = null;

        // recompute the edge collapse costs for neighboring vertices
        for (i = 0; i < tmp.Count; i++) {
            ComputeEdgeCostAtVertex(tmp[i]);
            //UnityEngine.Debug.Log("Collapse: ComputeEdgeCostAtVertex");
        }
    }

    public void AddVertex(List<Vector> vert) {
        for (int i = 0; i < vert.Count; i++) {
            Vertex v = new Vertex(vert[i], i);
            vertices.Add(v);
        }
    }

    public void AddFaces(List<tridata> tri) {
        for (int i = 0; i < tri.Count; i++) {
            Triangle t = new Triangle(vertices[tri[i].v[0]], vertices[tri[i].v[1]], vertices[tri[i].v[2]]);
            triangles.Add(t);
        }
    }
    
    public Vertex MinimumCostEdge() {
        // Find the edge that when collapsed will affect model the least.
        // This funtion actually returns a Vertex, the second vertex
        // of the edge (collapse candidate) is stored in the vertex data.
        // Serious optimization opportunity here: this function currently
        // does a sequential search through an unsorted list :-(
        // Our algorithm could be O(n*lg(n)) instead of O(n*n)
        Vertex mn = vertices[0];
        for (int i = 0; i < vertices.Count; i++) {
            if (vertices[i].objdist < mn.objdist) {
                mn = vertices[i];
            }
        }
        return mn;
    }

    public void ProgressiveMesh(ref List<Vector> vert, ref List<tridata> tri,
                                ref List<int> map, ref List<int> permutation) {
        AddVertex(vert); // put input data into our data structures
        AddFaces(tri);
        ComputeAllEdgeCollapseCosts(); // cache all edge collapse costs
        //UnityEngine.Debug.Log("Calculated edge costs");
        permutation.Capacity = this.vertices.Count; // pre-allocate space
        map.Capacity = this.vertices.Count; // pre-allocate space
        for (int vi=0; vi < this.vertices.Count; vi++) {
            permutation.Add(-999);
            map.Add(-999);
        }
        //UnityEngine.Debug.Log("Prefilled map and permutation lists");

        // reduce the object down to nothing:
        while (this.vertices.Count > 0) {
            // get the next vertex to collapse
            Vertex mn = MinimumCostEdge();
            // keep track of this vertex, i.e. the collapse ordering
            permutation[mn.id] = this.vertices.Count - 1;
            // keep track of vertex to which we collapse to
            map[this.vertices.Count - 1] = (mn.collapse != null) ? mn.collapse.id : -1;
            // Collapse this edge
            Collapse(mn, mn.collapse);
            //UnityEngine.Debug.Log("ProgressiveMesh: verticies = " + this.vertices.Count.ToString());
        }
        // reorder the map list based on the collapse ordering
        for (int i = 0; i < map.Count; i++) {
            map[i] = (map[i] == -1) ? 0 : permutation[map[i]];
        }
        // The caller of this function should reorder their vertices
        // according to the returned "permutation".
    }
}

} // close namespace