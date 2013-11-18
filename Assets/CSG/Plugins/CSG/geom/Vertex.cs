namespace CSG {

using UnityEngine;

/**
 *class Vertex
 * 
 * Represents a vertex of a polygon. Use your own vertex class instead of this
 * one to provide additional features like texture coordinates and vertex
 * colors. Custom vertex classes need to implement the IVertex interface.
 * 
 * @see com.floorplanner.csg.IVertex
 */
public class Vertex : IVertex
{
    public Vector3 pos {get; set; }
    public Vector3 normal;
    
    public Vertex(Vector3 pos) {
        this.pos = pos != null ? pos : Vector3.zero;
        this.normal = this.normal != null ? normal : Vector3.zero;
    }
    
    public Vertex(Vector3 pos, Vector3 normal) {
        this.pos = pos != null ? pos : Vector3.zero;
        this.normal = normal != null ? normal : Vector3.zero;
    }

    public IVertex clone() {
        return new Vertex(this.pos, this.normal);
    }
    
    public void flip() {
        this.normal *= -1f;
    }
    
    public IVertex interpolate(IVertex other, float t) {
        return new Vertex(
            lerp(this.pos, other.pos, t), 
            lerp(this.normal, (other as Vertex).normal, t)
        );
        
    }
    
    protected Vector3 lerp(Vector3 a, Vector3 b, float t) {
        Vector3 ab = b - a;
        ab *= t;
        return a + ab;
    }
}

}