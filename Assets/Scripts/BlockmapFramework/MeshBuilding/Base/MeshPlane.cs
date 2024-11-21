using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.MeshBuilding
{
    /// <summary>
    /// A mesh plane represents a simple rectangular plane. Consists of 4 vertices and 2 triangles.
    /// </summary>
    public class MeshPlane
    {
        public MeshVertex Vertex1;
        public MeshVertex Vertex2;
        public MeshVertex Vertex3;
        public MeshVertex Vertex4;

        public MeshTriangle Triangle1;
        public MeshTriangle Triangle2;

        public MeshPlane(MeshVertex vertex1, MeshVertex vertex2, MeshVertex vertex3, MeshVertex vertex4, MeshTriangle triangle1, MeshTriangle triangle2)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
            Vertex4 = vertex4;
            Triangle1 = triangle1;
            Triangle2 = triangle2;
        }
    }
}