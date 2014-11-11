using System.Collections.Generic;
using UnityEngine;

namespace ConstructiveSolidGeometry
{
    public class Plane
    {
        public static float EPSILON = 0.00001f;

        public const int COPLANAR = 0;
        public const int FRONT = 1;
        public const int BACK = 2;
        public const int SPANNING = 3;

        public Vector3 normal;
        public float w;

        public Plane(Vector3 normal, float w = 0.0f)
        {
            this.normal = normal;
            this.w = w;
        }

        public Plane()
        {
            this.w = 0f;
        }

        public Plane clone()
        {
            return new Plane(this.normal, this.w);
        }

        public void flip()
        {
            this.normal *= -1;
            this.w = -this.w;
        }

        /// <summary>
        /// Split `polygon` by this plane if needed, then put the polygon or polygon
        /// fragments in the appropriate lists. Coplanar polygons go into either
        /// `coplanarFront` or `coplanarBack` depending on their orientation with
        /// respect to this plane. Polygons in front or in back of this plane go into
        /// either `front` or `back`
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="coplanarFront"></param>
        /// <param name="coplanarBack"></param>
        /// <param name="front"></param>
        /// <param name="back"></param>
        public void splitPolygon(Polygon polygon,
                                 ref List<Polygon> coplanarFront,
                                 ref List<Polygon> coplanarBack,
                                 ref List<Polygon> front,
                                 ref List<Polygon> back)
        {
            //Debug.Log("splitPolygon: " + polygon.vertices[0].pos + ", " + polygon.vertices[1].pos + ", " + polygon.vertices[2].pos);

            IVertex[] vertices = polygon.vertices;
            int polygonType = 0;
            List<int> types = new List<int>();
            int type;
            float t;
            int i;

            for (i = 0; i < vertices.Length; i++)
            {
                t = Vector3.Dot(this.normal, vertices[i].pos) - this.w;
                if (t < -Plane.EPSILON) { type = BACK; } else if (t > Plane.EPSILON) { type = FRONT; } else { type = COPLANAR; }
                polygonType |= type;
                types.Add(type);
            }

            // Put the polygon in the correct list, splitting it when necessary.
            switch (polygonType)
            {
                case COPLANAR:
                    if (Vector3.Dot(this.normal, polygon.plane.normal) > 0)
                    {
                        coplanarFront.Add(polygon);
                    }
                    else
                    {
                        coplanarBack.Add(polygon);
                    }
                    break;
                case FRONT:
                    front.Add(polygon);
                    break;
                case BACK:
                    back.Add(polygon);
                    break;
                default:
                case SPANNING:
                    if (polygonType != SPANNING)
                        Debug.Log("Defaulting to spanning");

                    List<IVertex> f = new List<IVertex>();
                    List<IVertex> b = new List<IVertex>();
                    for (i = 0; i < vertices.Length; i++)
                    {
                        int j = (i + 1) % vertices.Length;
                        int ti = types[i];
                        int tj = types[j];
                        IVertex vi = vertices[i];
                        IVertex vj = vertices[j];
                        if (ti != BACK) f.Add(vi);
                        if (ti != FRONT) b.Add(ti != BACK ? vi.clone() : vi);
                        if ((ti | tj) == SPANNING)
                        {
                            t = (this.w - Vector3.Dot(this.normal, vi.pos)) /
                                 Vector3.Dot(this.normal, vj.pos - vi.pos);
                            IVertex v = vi.interpolate(vj, t);
                            f.Add(v);
                            b.Add(v.clone());
                        }
                    }
                    if (f.Count >= 3) front.Add(new Polygon(f, polygon.shared));
                    if (b.Count >= 3) back.Add(new Polygon(b, polygon.shared));
                    break;
            }
        }

        public static Plane fromPoints(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross((b - a), (c - a));
            n.Normalize();
            return new Plane(n, Vector3.Dot(n, a));
        }
    }
}