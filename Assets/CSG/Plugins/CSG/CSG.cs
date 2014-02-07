using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Constructive Solid Geometry (CSG) is a modeling technique that uses Boolean
// operations like union and intersection to combine 3D solids. This library
// implements CSG operations on meshes elegantly and concisely using BSP trees,
// and is meant to serve as an easily understandable implementation of the
// algorithm. All edge cases involving overlapping coplanar polygons in both
// solids are correctly handled.
// 
// Example usage:
// 
//     var cube = CSG.cube();
//     var sphere = CSG.sphere({ radius: 1.3 });
//     var polygons = cube.subtract(sphere).toPolygons();
// 
// ## Implementation Details
// 
// All CSG operations are implemented in terms of two functions, `clipTo()` and
// `invert()`, which remove parts of a BSP tree inside another BSP tree and swap
// solid and empty space, respectively. To find the union of `a` and `b`, we
// want to remove everything in `a` inside `b` and everything in `b` inside `a`,
// then combine polygons from `a` and `b` into one solid:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     a.build(b.allPolygons());
// 
// The only tricky part is handling overlapping coplanar polygons in both trees.
// The code above keeps both copies, but we need to keep them in one tree and
// remove them in the other tree. To remove them from `b` we can clip the
// inverse of `b` against `a`. The code for union now looks like this:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     b.invert();
//     b.clipTo(a);
//     b.invert();
//     a.build(b.allPolygons());
// 
// Subtraction and intersection naturally follow from set operations. If
// union is `A | B`, subtraction is `A - B = ~(~A | B)` and intersection is
// `A & B = ~(~A | ~B)` where `~` is the complement operator.
// 
// ## License
// 
// Original ActionScript version copyright (c) 2011 Evan Wallace (http://madebyevan.com/), under the MIT license.
// Ported to C# / Unity by Andrew Perry, 2013.

namespace CombinedStructureGenerator
{
    /// <summary>
    /// Holds a binary space partition tree representing a 3D solid. Two solids can
    /// be combined using the `union()`, `subtract()`, and `intersect()` methods.
    /// </summary>
    public class CSG
    {
        public List<Polygon> polygons;
        private Bounds bounds = new Bounds();
        
        /// <summary>
        /// Constuctor
        /// </summary>
        public CSG()
        {
            this.polygons = new List<Polygon>();
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public CSG clone()
        {
            CSG csg = new CSG();
            foreach (Polygon p in this.polygons)
            {
                csg.polygons.Add(p.clone());
            }
            return csg;
        }

        public CSG inverse()
        {
            CSG csg = this.clone();
            foreach (Polygon p in csg.polygons)
            {
                p.flip();
            }
            return csg;
        }

        public List<Polygon> toPolygons()
        {
            return this.polygons;
        }

        public Mesh toMesh()
        {
            List<Polygon> trisFromQuads = new List<Polygon>();
            // replace higher order polygons with tris
            for (int i = this.polygons.Count - 1; i >= 0; i--)
            {
                if (this.polygons[i].vertices.Length > 3)
                {
                    //Debug.Log("!!! Poly to Tri (order): " + this.polygons[i].vertices.Length);
                    for (int vi = 1; vi < this.polygons[i].vertices.Length - 1; vi++)
                    {
                        IVertex[] tri = new IVertex[] {this.polygons[i].vertices[0], 
                                                   this.polygons[i].vertices[vi], 
                                                   this.polygons[i].vertices[vi+1]};
                        trisFromQuads.Add(new Polygon(tri));
                    }
                    this.polygons.RemoveAt(i);
                }
            }
            this.polygons.AddRange(trisFromQuads);

            Mesh m = new Mesh();
            Vector3[] mverts = new Vector3[this.polygons.Count * 3];
            Vector3[] mnormals = new Vector3[this.polygons.Count * 3];
            int[] mtris = new int[this.polygons.Count * 3];
            int tri_index = 0;
            foreach (Polygon tri in this.polygons)
            {
                //sDebug.Log("toMesh tri: " + tri.vertices[0].pos + ", " +tri.vertices[1].pos + ", " + tri.vertices[2].pos);

                mverts[tri_index] = tri.vertices[0].pos;
                mverts[tri_index + 1] = tri.vertices[1].pos;
                mverts[tri_index + 2] = tri.vertices[2].pos;

                mnormals[tri_index] = (tri.vertices[0] as Vertex).normal;
                mnormals[tri_index + 1] = (tri.vertices[1] as Vertex).normal;
                mnormals[tri_index + 2] = (tri.vertices[2] as Vertex).normal;

                mtris[tri_index] = tri_index;
                mtris[tri_index + 1] = tri_index + 1;
                mtris[tri_index + 2] = tri_index + 2;
                tri_index += 3;
            }
            m.vertices = mverts;
            m.normals = mnormals;
            m.triangles = mtris;

            //Debug.Log("toMesh verts, normals, tris: " + m.vertices.Length + ", " +m.normals.Length+", "+m.triangles.Length);

            return m;
        }

        public static CSG fromMesh(Mesh m, Transform tf)
        {
            List<Polygon> triangles = new List<Polygon>();
            int[] tris = m.triangles;
            Debug.Log("tris " + tris.Length);
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vertex[] vs = new Vertex[3];
                vs[0] = TranslateVertex(m, tf, tris[t]);
                vs[1] = TranslateVertex(m, tf, tris[t + 1]);
                vs[2] = TranslateVertex(m, tf, tris[t + 2]);
                //Debug.Log("Tri index: " + (t+i).ToString() + ", Vertex: " + vs[i].pos);
                triangles.Add(new Polygon(vs));
            }
            Debug.Log("Poly " + triangles.Count);
            return CSG.fromPolygons(triangles);
        }

        private static Vertex TranslateVertex(Mesh m, Transform tf, int tri)
        {
            return new Vertex(Vector3.Scale(m.vertices[tri], tf.localScale) + tf.position, m.normals[tri]);
        }
        
        /// <summary>
        ///Return a new CSG solid representing space in either this solid or in the
        ///solid `csg`. Neither this solid nor the solid `csg` are modified.
        ///
        ///    A.union(B)
        ///
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |       +----+
        ///    +----+--+    |       +----+       |
        ///         |   B   |            |       |
        ///         |       |            |       |
        ///         +-------+            +-------+
        /// </summary>
        /// <param name="csg"></param>
        /// <returns></returns>
        public CSG union(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            a.clipTo(b);
            b.clipTo(a);
            //b.invert();
            //b.clipTo(a);
            //b.invert();
            a.build(b.allPolygons());
            return CSG.fromPolygons(a.allPolygons());
        }

        /// <summary>
        /// Return a new CSG solid representing space in this solid but not in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// A.subtract(B)
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |    +--+
        ///    +----+--+    |       +----+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </summary>
        /// <param name="csg"></param>
        /// <returns></returns>
        public CSG subtract(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            //Debug.Log(this.clone().polygons.Count + " -- " + csg.clone().polygons.Count);
            //Debug.Log("CSG.subtract: Node a = " + a.polygons.Count + " polys, Node b = " + b.polygons.Count + " polys.");
            a.invert();
            a.clipTo(b);
            b.clipTo(a);
            b.invert();
            b.clipTo(a);
            b.invert();
            a.build(b.allPolygons());
            a.invert();
            return CSG.fromPolygons(a.allPolygons());
        }

        /// <summary>
        /// Return a new CSG solid representing space both this solid and in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        ///     A.intersect(B)
        /// 
        ///    +-------+
        ///    |       |
        ///    |   A   |
        ///    |    +--+----+   =   +--+
        ///    +----+--+    |       +--+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </summary>
        /// <param name="csg"></param>
        /// <returns>CSG of the intersection</returns>
        public CSG intersect(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            a.invert();
            b.invert();
            a.clipTo(b);
            b.clipTo(a);
            a.build(b.allPolygons());
            return CSG.fromPolygons(a.allPolygons()).inverse();
        }

        /// <summary>
        /// Cube function, Untested but compiles
        /// </summary>
        /// <param name="c">center</param>
        /// <param name="r">radius</param>
        /// <returns></returns>
        public static CSG cube(Vector3 c, Vector3 r)
        {
            //TODO: Test if this works
            Polygon[] polygons = new Polygon[6];
            int[][][] data = new int [][][] {
                new int[][]{new int[]{0, 4, 6, 2}, new int[]{-1, 0, 0}},
                new int[][]{new int[]{1, 3, 7, 5}, new int[]{1, 0, 0}},
                new int[][]{new int[]{0, 1, 5, 4}, new int[]{0, -1, 0}},
                new int[][]{new int[]{2, 6, 7, 3}, new int[]{0, 1, 0}},
                new int[][]{new int[]{0, 2, 3, 1}, new int[]{0, 0, -1}},
                new int[][]{new int[]{4, 5, 7, 6}, new int[]{0, 0, 1}}
            };
            for(int x = 0; x < 6; x++) {
                int[][] v = data[x];
                Vector3 normal = new Vector3((float)v[1][0], (float)v[1][1], (float)v[1][2]);

                IVertex[] verts = new IVertex[4];
                for(int i = 0; i< 4; i++)
                {
                    verts[i] = new Vertex(
                        new Vector3(
                            c.x + (r.x * (2 * (((i & 1) > 0)?1:0) - 1)),
                            c.y + (r.y * (2 * (((i & 2) > 0)?1:0) - 1)),
                            c.z + (r.z * (2 * (((i & 4) > 0)?1:0) - 1))),
                            normal
                        );
                }
                polygons[x] = new Polygon(verts);
            }
            return CSG.fromPolygons(polygons);
        }

        private static void makeSphereVertex(ref List<IVertex> vxs, Vector3 center, float r, float theta, float phi)
        {
            theta *= Mathf.PI * 2;
            phi *= Mathf.PI;
            Vector3 dir = new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi),
                                      Mathf.Cos(phi),
                                      Mathf.Sin(theta) * Mathf.Sin(phi)
            );
            Vector3 sdir = dir;
            sdir *= r;
            vxs.Add(new Vertex(center + sdir, dir));
        }

        public static CSG sphere(Vector3 center, float radius = 1, float slices = 16f, float stacks = 8f)
        {
            float r = radius;
            List<Polygon> polygons = new List<Polygon>();
            List<IVertex> vertices;

            for (int i = 0; i < slices; i++)
            {
                for (int j = 0; j < stacks; j++)
                {
                    vertices = new List<IVertex>();
                    makeSphereVertex(ref vertices, center, r, i / slices, j / stacks);
                    if (j > 0) makeSphereVertex(ref vertices, center, r, (i + 1) / slices, j / stacks);
                    if (j < stacks - 1) makeSphereVertex(ref vertices, center, r, (i + 1) / slices, (j + 1) / stacks);
                    makeSphereVertex(ref vertices, center, r, i / slices, (j + 1) / stacks);
                    polygons.Add(new Polygon(vertices));
                }
            }
            return CSG.fromPolygons(polygons);
        }

        /// <summary>
        /// Construct a CSG solid from a list of `Polygon` instances.
        /// The polygons are cloned
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static CSG fromPolygons(List<Polygon> polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            foreach (Polygon p in polygons)
            {
                csg.polygons.Add(p.clone());
            }

            return csg;
        }

        /// <summary>
        /// Create CSG from array, does not clone the polygons
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static CSG fromPolygons(Polygon[] polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            csg.polygons.AddRange(polygons);
            return csg;
        }
    }

}
