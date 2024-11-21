using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.MeshBuilding
{
    public class MeshTriangle
    {
        public MeshVertex Vertex1;
        public MeshVertex Vertex2;
        public MeshVertex Vertex3;
        public int SubmeshIndex;

        public MeshTriangle(int submeshIndex, MeshVertex vertex1, MeshVertex vertex2, MeshVertex vertex3)
        {
            SubmeshIndex = submeshIndex;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
        }

        public List<MeshVertex> GetVertices()
        {
            return new List<MeshVertex>() { Vertex1, Vertex2, Vertex3 };
        }
    }
}