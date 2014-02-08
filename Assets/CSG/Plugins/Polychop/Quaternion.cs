namespace Polychop {

using System;

public class Quaternion {
    public float r;
    public float x;
    public float y;
    public float z;

    public Quaternion() {
        x = y = z = 0.0f;
        r = 1.0f;
    }
    public Quaternion(Vector v,float t) {
        v = Vector.normalize(v);
        r = (float)Math.Cos(t / 2.0);
        v = v * (float)Math.Sin(t / 2.0);
        x = v.x;
        y = v.y;
        z = v.z;
    }
    public Quaternion(float _r, float _x, float _y, float _z) {
        r = _r;
        x = _x;
        y = _y;
        z = _z;
    }

    public float angle() {
        return (float)(Math.Acos(r) * 2.0);
    }
    public Vector axis()
    {
        Vector a = new Vector(x, y, z);
        return a * (float)(1 / Math.Sin(angle() / 2.0));
    }
    public Vector xdir()
    {
        return new Vector(1 - 2 * (y * y + z * z), 2 * (x * y + r * z), 2 * (x * z - r * y));
    }
    public Vector ydir()
    {
        return new Vector(2 * (x * y - r * z),1 - 2 * (x * x + z * z), 2 * (y * z + r * x));
    }
    public Vector zdir()
    {
        return new Vector(2 * (x * z + r * y), 2 * (y * z - r * x),1 - 2 * (x * x + y * y));
    }
    public matrix getmatrix()
    {
        return new matrix(xdir(), ydir(), zdir());
    }

    //operator matrix(){return getmatrix();}

    //Quaternion Implementation
    public static Quaternion operator * (Quaternion a, Quaternion b)
    {
        Quaternion c = new Quaternion();
        c.r = a.r * b.r - a.x * b.x - a.y * b.y - a.z * b.z;
        c.x = a.r * b.x + a.x * b.r + a.y * b.z - a.z * b.y;
        c.y = a.r * b.y - a.x * b.z + a.y * b.r + a.z * b.x;
        c.z = a.r * b.z + a.x * b.y - a.y * b.x + a.z * b.r;
        return c;
    }
    public static Quaternion operator - (Quaternion q)
    {
        return new Quaternion(q.r * -1, q.x, q.y, q.z);
    }
    public static Quaternion operator * (Quaternion a, float b)
    {
        return new Quaternion(a.r * b, a.x * b, a.y * b, a.z * b);
    }
    public static Vector operator * (Quaternion q, Vector v)
    {
        return q.getmatrix() * v;
    }
    public static Vector operator * (Vector v, Quaternion q)
    {
        //Debug.Assert(0); // must multiply with the quat on the left
        return new Vector(0.0f, 0.0f, 0.0f);
    }

    public static Quaternion operator + (Quaternion a, Quaternion b)
    {
        return new Quaternion(a.r + b.r, a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static float operator ^ (Quaternion a, Quaternion b)
    {
        return (a.r * b.r + a.x * b.x + a.y * b.y + a.z * b.z);
    }
    public Quaternion slerp(Quaternion a, Quaternion b, float interp)
    {
        if ((a ^ b) < 0.0)
        {
            a.r = -a.r;
            a.x = -a.x;
            a.y = -a.y;
            a.z = -a.z;
        }
        float theta = (float)Math.Acos(a ^ b);
        if (theta == 0.0f)
        {
            return (a);
        }
        return a * (float)(Math.Sin(theta - interp * theta) / Math.Sin(theta)) + b * (float)(Math.Sin(interp * theta) / Math.Sin(theta));
    }
}

} // close namespace
