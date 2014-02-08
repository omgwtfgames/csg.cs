namespace Polychop {

using System;
using System.Diagnostics;

public class tridata {
        public int[] v = new int[3]; // indices to vertex list
        // TODO: texture and vertex normal info removed for this demo - add them to Vertex class ?

        public tridata(int a, int b, int c) {
            v[0] = a;
            v[1] = b;
            v[2] = c;
        }
}

public class Vector {

     public float x;
     public float y;
     public float z;

     // overloaded methods
     //public Vector(float _x, float _y) : this(_x, _y, 0.0) { }
     //public Vector(float _x) : this(_x, 0.0, 0.0) { }

     public Vector() : this(0.0f, 0.0f, 0.0f) { }
     public Vector(float _x, float _y, float _z) {
         x = _x;
         y = _y;
         z = _z;
     }

    /*
     * TODO: don't know what this does
     public static implicit operator float (Vector ImpliedObject) {
         return &x;
     }
     */

    public static float magnitude(Vector v)
    {
        return (float)Math.Sqrt(v.x*v.x + v.y*v.y + v.z*v.z);
    }

    public static Vector normalize(Vector v)
    {
        float d = magnitude(v);
        if (d == 0)
        {
            //Console.Write("Cant normalize ZERO vector\n");
            //Debug.Assert(0);
            d = 0.1f;
        }
        v.x /= d;
        v.y /= d;
        v.z /= d;
        return v;
    }

    public static  Vector operator + (Vector v1, Vector v2)
    {
        return new Vector(v1.x + v2.x,v1.y + v2.y,v1.z + v2.z);
    }
    public static  Vector operator - (Vector v1, Vector v2)
    {
        return new Vector(v1.x - v2.x,v1.y - v2.y,v1.z - v2.z);
    }
    public static Vector operator - (Vector v)
    {
        return new Vector(-v.x,-v.y,-v.z);
    }
    public static Vector operator * (Vector v1, float s)
    {
        return new Vector(v1.x * s,v1.y * s,v1.z * s);
    }
    public static Vector operator * (float s, Vector v1)
    {
        return new Vector(v1.x * s,v1.y * s,v1.z * s);
    }
    public static Vector operator / (Vector v1, float s)
    {
        return v1 * (1.0f / s);
    }
    public static float operator ^ (Vector v1, Vector v2)
    {
        return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
    }
    public static Vector operator * (Vector v1, Vector v2)
    {
        return new Vector(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
    }
    public Vector planelineintersection(Vector n, float d, Vector p1, Vector p2)
    {
        // returns the point where the line p1-p2 intersects the plane n&d
            Vector dif = p2 - p1;
            float dn = n ^ dif;
            float t = -(d + (n ^ p1)) / dn;
            return p1 + (dif * t);
    }
    public bool concurrent(Vector a, Vector b)
    {
        return (a.x == b.x && a.y == b.y && a.z == b.z);
    }
}

} // close namespace