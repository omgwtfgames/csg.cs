namespace Polychop {

public class matrix {

     public Vector x = new Vector();
     public Vector y = new Vector();
     public Vector z = new Vector();

     public matrix() {
         x = new Vector(1.0f, 0.0f, 0.0f);
         y = new Vector(0.0f, 1.0f, 0.0f);
         z = new Vector(0.0f, 0.0f, 1.0f);
     }

     public matrix(Vector _x, Vector _y, Vector _z) {
         x = _x;
         y = _y;
         z = _z;
     }

    // Matrix Implementation
    public static matrix transpose(matrix m) {
        return new matrix(new Vector(m.x.x,m.y.x,m.z.x), new Vector(m.x.y,m.y.y,m.z.y), new Vector(m.x.z,m.y.z,m.z.z));
    }

    public static Vector operator * (matrix m,Vector v) {
        m = matrix.transpose(m); // since column ordered
        return new Vector(m.x ^ v,m.y ^ v,m.z ^ v);
    }

    public static matrix operator * (matrix m1,matrix m2) {
        m1 = matrix.transpose(m1);
        return new matrix(m1 * m2.x,m1 * m2.y,m1 * m2.z);
    }
}

} // close namespace