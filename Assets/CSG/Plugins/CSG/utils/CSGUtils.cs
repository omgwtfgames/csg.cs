using UnityEngine;
using System.Collections.Generic;

namespace ConstructiveSolidGeometry
{
    public class CSGUtils
    {
        /// <summary>
        /// Creates a Polygon from an array of points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="shared"></param>
        /// <returns></returns>
        public static Polygon createPolygon(Vector3[] points, System.Object shared = null)
        {
            if (points.Length < 2)
            {
                return null;
            }

            List<IVertex> vertices = new List<IVertex>();
            foreach (Vector3 pos in points)
            {
                vertices.Add(new Vertex(pos));
            }
            Polygon polygon = new Polygon(vertices, shared);

            return polygon;
        }

        /// <summary>
        /// Extrudes a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to extrude</param>
        /// <param name="distance">Extrusion distance</param>
        /// <param name="normal">Optional normal to extrude along, default is polygon normal</param>
        /// <returns></returns>
        public static List<Polygon> extrudePolygon(Polygon polygon, float distance, Vector3? normal = null)
        {
            normal = normal != null ? normal : polygon.plane.normal;

            Vector3 du = normal.GetValueOrDefault();
            IVertex[] vertices = polygon.vertices;
            List<IVertex> top = new List<IVertex>();
            List<IVertex> bot = new List<IVertex>();
            List<Polygon> polygons = new List<Polygon>();
            Vector3 invNormal = normal.GetValueOrDefault();

            du *= distance;
            invNormal *= -1f;

            for (int i = 0; i < vertices.Length; i++)
            {
                int j = (i + 1) % vertices.Length;
                Vector3 p1 = vertices[i].pos;
                Vector3 p2 = vertices[j].pos;
                Vector3 p3 = p2 + du;
                Vector3 p4 = p1 + du;
                Plane plane = Plane.fromPoints(p1, p2, p3);
                Vertex v1 = new Vertex(p1, plane.normal);
                Vertex v2 = new Vertex(p2, plane.normal);
                Vertex v3 = new Vertex(p3, plane.normal);
                Vertex v4 = new Vertex(p4, plane.normal);
                Polygon poly = new Polygon(new List<IVertex>(new IVertex[] { v1, v2, v3, v4 }), polygon.shared);
                polygons.Add(poly);
                top.Add(new Vertex(p4, normal.GetValueOrDefault()));
                bot.Insert(0, new Vertex(p1, invNormal));
            }

            polygons.Add(new Polygon(top, polygon.shared));
            polygons.Add(new Polygon(bot, polygon.shared));

            return polygons;
        }
    }
}