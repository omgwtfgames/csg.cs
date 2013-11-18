namespace CSG {

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/** 
     * Constructive Solid Geometry (CSG) is a modeling technique that uses Boolean
     * operations like union and intersection to combine 3D solids. This library
     * implements CSG operations on meshes elegantly and concisely using BSP trees,
     * and is meant to serve as an easily understandable implementation of the
     * algorithm. All edge cases involving overlapping coplanar polygons in both
     * solids are correctly handled.
     * 
     * Example usage:
     * 
     *     var cube = CSG.cube();
     *     var sphere = CSG.sphere({ radius: 1.3 });
     *     var polygons = cube.subtract(sphere).toPolygons();
     * 
     * ## Implementation Details
     * 
     * All CSG operations are implemented in terms of two functions, `clipTo()` and
     * `invert()`, which remove parts of a BSP tree inside another BSP tree and swap
     * solid and empty space, respectively. To find the union of `a` and `b`, we
     * want to remove everything in `a` inside `b` and everything in `b` inside `a`,
     * then combine polygons from `a` and `b` into one solid:
     * 
     *     a.clipTo(b);
     *     b.clipTo(a);
     *     a.build(b.allPolygons());
     * 
     * The only tricky part is handling overlapping coplanar polygons in both trees.
     * The code above keeps both copies, but we need to keep them in one tree and
     * remove them in the other tree. To remove them from `b` we can clip the
     * inverse of `b` against `a`. The code for union now looks like this:
     * 
     *     a.clipTo(b);
     *     b.clipTo(a);
     *     b.invert();
     *     b.clipTo(a);
     *     b.invert();
     *     a.build(b.allPolygons());
     * 
     * Subtraction and intersection naturally follow from set operations. If
     * union is `A | B`, subtraction is `A - B = ~(~A | B)` and intersection is
     * `A & B = ~(~A | ~B)` where `~` is the complement operator.
     * 
     * ## License
     * 
     * Original ActionScript version copyright (c) 2011 Evan Wallace (http://madebyevan.com/), under the MIT license.
     * Ported to C# / Unity by Andrew Perry, 2013.
     * 
     * class CSG
     *
     * Holds a binary space partition tree representing a 3D solid. Two solids can
     * be combined using the `union()`, `subtract()`, and `intersect()` methods.
     */ 
public class CSG {
    public List<Polygon> polygons;
    
    /**
     * Constructor
     */ 
    public CSG() {
        this.polygons = new List<Polygon>();
    }
    
    /**
     * Clone
     */ 
    public CSG clone() {
        CSG csg = new CSG();
        foreach (Polygon p in this.polygons) {
            csg.polygons.Add(p);
        }
        return csg;
    }
    
    public CSG inverse() {
        CSG csg = this.clone();
        foreach (Polygon p in csg.polygons) {
            p.flip();
        }
        return csg;
    }
    
    public List<Polygon> toPolygons() 
    {
        return this.polygons;    
    }
    
    public Mesh toMesh() {
        
        List<Polygon> trisFromQuads = new List<Polygon>();
        // replace higher order polygons with tris
        for (int i = this.polygons.Count - 1; i >= 0; i--) {
            if (this.polygons[i].vertices.Length > 3) {
                //Debug.Log("!!! Poly to Tri (order): " + this.polygons[i].vertices.Length);
                for (int vi=1; vi < this.polygons[i].vertices.Length - 1; vi++) {
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
        foreach (Polygon tri in this.polygons) {
            Debug.Log("toMesh tri: " + tri.vertices[0].pos + ", " +tri.vertices[1].pos + ", " + tri.vertices[2].pos);
            
            mverts[tri_index] = tri.vertices[0].pos;
            mverts[tri_index+1] = tri.vertices[1].pos;
            mverts[tri_index+2] = tri.vertices[2].pos;
            
            mnormals[tri_index] = (tri.vertices[0] as Vertex).normal;
            mnormals[tri_index+1] = (tri.vertices[1] as Vertex).normal;
            mnormals[tri_index+2] = (tri.vertices[2] as Vertex).normal;
            
            mtris[tri_index] = tri_index;
            mtris[tri_index + 1] = tri_index + 1;
            mtris[tri_index + 2] = tri_index + 2;
            tri_index += 3;
        }
        m.vertices = mverts;
        m.normals = mnormals;
        m.triangles = mtris;
        
        Debug.Log("toMesh verts, normals, tris: " + m.vertices.Length + ", " +m.normals.Length+", "+m.triangles.Length);
        
        return m;
    }
    
    public static CSG fromMesh(Mesh m, Transform tf) {
        List<Polygon> triangles = new List<Polygon>();
        Polygon p;
        int[] tris = m.triangles;
        for (int t=0; t < tris.Length; t+=3) {
            Vertex[] vs = new Vertex[3];
            for (int i = 0; i < 3; i++) {
                vs[i] = new Vertex(Vector3.Scale(m.vertices[tris[t+i]], tf.localScale) + tf.position, m.normals[tris[t+i]]);
                Debug.Log("Tri index: " + (t+i).ToString() + ", Vertex: " + vs[i].pos);
            }
            p = new Polygon(vs);
            triangles.Add(p);
        }
        return CSG.fromPolygons(triangles);
    }
    
    
    /**
      * Return a new CSG solid representing space in either this solid or in the
      * solid `csg`. Neither this solid nor the solid `csg` are modified.
      * 
      *     A.union(B)
      * 
      *     +-------+            +-------+
      *     |       |            |       |
      *     |   A   |            |       |
      *     |    +--+----+   =   |       +----+
      *     +----+--+    |       +----+       |
      *          |   B   |            |       |
      *          |       |            |       |
      *          +-------+            +-------+
      * 
      * @param csg
      * 
      * @return CSG
      */
    public CSG union(CSG csg)
    {
        Node a = new Node(this.clone().polygons);
        Node b = new Node(csg.clone().polygons);
        a.clipTo(b);
        b.clipTo(a);
        b.invert();
        b.clipTo(a);
        b.invert();
        a.build(b.allPolygons());
        return CSG.fromPolygons(a.allPolygons());
    }
    
    /** 
     * Return a new CSG solid representing space in this solid but not in the
     * solid `csg`. Neither this solid nor the solid `csg` are modified.
     * 
     *     A.subtract(B)
     * 
     *     +-------+            +-------+
     *     |       |            |       |
     *     |   A   |            |       |
     *     |    +--+----+   =   |    +--+
     *     +----+--+    |       +----+
     *          |   B   |
     *          |       |
     *          +-------+
     * 
     * @param csg
     * 
     * @return CSG
     */
    public CSG subtract(CSG csg)
    {
        Node a = new Node(this.clone().polygons);
        Node b = new Node(csg.clone().polygons);
        //Debug.Log(this.clone().polygons.Count + " -- " + csg.clone().polygons.Count);
        Debug.Log("CSG.subtract: Node a = " + a.polygons.Count + " polys, Node b = " + b.polygons.Count + " polys.");
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
    
    /** 
     * Return a new CSG solid representing space both this solid and in the
     * solid `csg`. Neither this solid nor the solid `csg` are modified.
     * 
     *     A.intersect(B)
     * 
     *     +-------+
     *     |       |
     *     |   A   |
     *     |    +--+----+   =   +--+
     *     +----+--+    |       +--+
     *          |   B   |
     *          |       |
     *          +-------+
     * 
     * @param csg
     * 
     * @return CSG
     */ 
    public CSG intersect(CSG csg)
    {
        Node a = new Node(this.clone().polygons);
        Node b = new Node(csg.clone().polygons);
        a.invert();
        b.clipTo(a);
        b.invert();
        a.clipTo(b);
        b.clipTo(a);
        a.build(b.allPolygons());
        a.invert();
        return CSG.fromPolygons(a.allPolygons());
    }
    
    /**
     * Cube
     * 
     * @param center
     * @param radius
     * 
     * @return CSG
     */ 
    
    /*
    public static CSG cube(Vector3D? center=null, Vector3? radius=null)
    {
        Vector3 c = centre != null ? center : Vector3.zero;
        Vector3 r = radius != null ? center : Vector3.one;
        List<Polygon> polygons = new List<Polygon>();
        int[][] data = new int [][] {
                {{0, 4, 6, 2}, {-1, 0, 0}},
                {{1, 3, 7, 5}, {1, 0, 0}},
                {{0, 1, 5, 4}, {0, -1, 0}},
                {{2, 6, 7, 3}, {0, 1, 0}},
                {{0, 2, 3, 1}, {0, 0, -1}},
                {{4, 5, 7, 6}, {0, 0, 1}}
            };
        foreach (int[] array in data) {
            Array v = array[0],
                Vector3 n = new Vector3((float)array[1][0], (float)array[1][1], (float)array[1][2]);
                // TODO: Wah !?!?! I don't really know Actionscript, no do I want to learn. 
                //       Unity has a prefab cube, use that.
                verts:Array = v.map(function(elem:*, index:int, a:Array):IVertex {
                        var i:int = elem as int;
                        return new Vertex(new Vector3D(
                            c.x + (r.x * (2 * ((i & 1)?1:0) - 1)),
                            c.y + (r.y * (2 * ((i & 2)?1:0) - 1)),
                            c.z + (r.z * (2 * ((i & 4)?1:0) - 1))),
                            n
                        );
                    });
            polygons.Add(new Polygon(Vector.<IVertex>(verts)));
        }
        return CSG.fromPolygons(polygons);
    }
    */
    
    /**
     * Sphere
     * 
     * @param center
     * @param radius
     * @param slices
     * @param stacks
     * 
     * @return CSG
     */ 
    
     private static void makeSphereVertex(ref List<IVertex> vxs, Vector3 center, float r, float theta, float phi) {
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
    
    public static CSG sphere(Vector3 center, float radius=1, float slices=16f, float stacks=8f)
    {
        Vector3 c = center != null ? center : Vector3.zero;
        float r = radius;
        List<Polygon> polygons = new List<Polygon>();
        List<IVertex> vertices;
        
        for (int i = 0; i < slices; i++) {
            for (int j = 0; j < stacks; j++) {
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
    
    /**
     * Construct a CSG solid from a list of `Polygon` instances.
     * 
     * @param polygons
     * 
     * @return CSG
     */ 
    public static CSG fromPolygons(List<Polygon> polygons)
    {
        CSG csg = new CSG();
        csg.polygons = polygons;
        return csg;
    }
}

}
