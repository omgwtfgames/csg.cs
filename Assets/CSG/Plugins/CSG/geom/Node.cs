using UnityEngine;
using System.Collections.Generic;

namespace ConstructiveSolidGeometry
{
    public class Node
    {
        public Plane plane;
        public Node front;
        public Node back;
        public List<Polygon> polygons = new List<Polygon>(); // TODO: optimization: this could probably be an array

        public Node(List<Polygon> polys)
        {
            List<Polygon> tpoly = new List<Polygon>();
            foreach (Polygon p in polys)
                tpoly.Add(p.clone());
            build(tpoly);
        }

        public Node()
        {
            //this.polygons = new List<Polygon>();
        }

        public Node clone()
        {
            Node node = new Node();
            if (this.plane != null) node.plane = this.plane.clone();
            if (this.front != null) node.front = this.front.clone();
            if (this.back != null) node.back = this.back.clone();
            foreach (Polygon p in this.polygons)
            {
                node.polygons.Add(p.clone());
            }
            return node;
        }

        /// <summary>
        /// Convert solid space to empty space and empty space to solid space.
        /// </summary>
        public void invert()
        {
            if (this.polygons.Count == 0)
            {
                Debug.LogError("No polygons?");
                return;
            }

            for (int i = 0; i < this.polygons.Count; i++)
            {
                this.polygons[i].flip();
            }
            this.plane.flip();
            if (this.front != null) this.front.invert();
            if (this.back != null) this.back.invert();
            Node temp = this.front;
            this.front = this.back;
            this.back = temp;
        }

        /// <summary>
        /// Recursively remove all polygons in `polygons` that are inside this BSP tree.
        /// </summary>
        /// <param name="polys"></param>
        /// <returns></returns>
        public List<Polygon> clipPolygons(List<Polygon> polys)
        {
            if (this.plane == null) return new List<Polygon>(polys);
            List<Polygon> front = new List<Polygon>();
            List<Polygon> back = new List<Polygon>();
            for (int i = 0; i < polys.Count; i++)
            {
                this.plane.splitPolygon(polys[i], ref front, ref back, ref front, ref back);
            }
            if (this.front != null) front = this.front.clipPolygons(front);
            if (this.back != null)
            {
                back = this.back.clipPolygons(back);
            }
            else { back.Clear(); }
            front.AddRange(back);
            return front;
        }

        /// <summary>
        /// Remove all polygons in this BSP tree that are inside the other BSP tree `bsp`.
        /// </summary>
        /// <param name="bsp"></param>
        public void clipTo(Node bsp)
        {
            this.polygons = bsp.clipPolygons(this.polygons);
            if (this.front != null) this.front.clipTo(bsp);
            if (this.back != null) this.back.clipTo(bsp);
        }

        /// <summary>
        /// Return a list of all polygons in this BSP tree.
        /// </summary>
        /// <returns></returns>
        public List<Polygon> allPolygons()
        {
            List<Polygon> polys = new List<Polygon>(this.polygons);
            if (this.front != null) polys.AddRange(this.front.allPolygons());
            if (this.back != null) polys.AddRange(this.back.allPolygons());
            return polys;
        }

        /// <summary>
        /// Build a BSP tree out of `polygons`. When called on an existing tree, the
        /// new polygons are filtered down to the bottom of the tree and become new
        /// nodes there. Each set of polygons is partitioned using the first polygon
        /// (no heuristic is used to pick a good split).
        /// </summary>
        /// <param name="polys"></param>
        /// <param name="stack"></param>
        public void build(List<Polygon> polys, int stack = 6000)
        {
            if (stack < 0)
            {
                Debug.LogWarning("Stack overflow prevented");
                return;
            }
            if (polys.Count == 0) return;
            if (this.plane == null) this.plane = polys[0].plane.clone();
            List<Polygon> tfront = new List<Polygon>();
            List<Polygon> tback = new List<Polygon>();
            for (int i = 0; i < polys.Count; i++)
            {
                this.plane.splitPolygon(polys[i], ref this.polygons, ref this.polygons, ref tfront, ref tback);
            }
            if (tfront.Count > 0)
            {
                if (this.front == null) this.front = new Node();
                this.front.build(tfront, stack - 1);
            }
            if (tback.Count > 0)
            {
                if (this.back == null) this.back = new Node();
                this.back.build(tback, stack - 1);
            }
        }
    }
}