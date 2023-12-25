using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshBuilder
{
    private GameObject MeshObject;

    public List<MeshVertex> Vertices = new List<MeshVertex>(); // Vertices and uv's are shared across all submeshes
    private List<List<MeshTriangle>> Triangles = new List<List<MeshTriangle>>(); // Each list contains the triangles of one submesh
    public int CurrentSubmesh = -1;

    private List<Material> Materials = new List<Material>(); // Stores the materials for each submesh

    /// <summary>
    /// Create a mesh builder for a new game object
    /// </summary>
    public MeshBuilder(string name, string layer, Transform parent = null)
    {
        MeshObject = new GameObject(name);
        MeshObject.layer = LayerMask.NameToLayer(layer);
        if (parent != null) MeshObject.transform.SetParent(parent);
        MeshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = MeshObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }

    /// <summary>
    /// Create a mesh builder for an existing object you want to add/modify the mesh. Material is given for the first submesh (submeshId = 0)
    /// </summary>
    public MeshBuilder(GameObject meshObject, Material material)
    {
        MeshObject = meshObject;
        AddNewSubmesh(material);
    }

    /// <summary>
    /// Create a mesh builder for an existing gameobject.
    /// </summary>
    public MeshBuilder(GameObject meshObject)
    {
        MeshObject = meshObject;
    }

    public GameObject ApplyMesh(bool addCollider = true, bool applyMaterials = true)
    {
        // Set index values for all vertices
        for (int i = 0; i < Vertices.Count; i++) Vertices[i].Id = i;

        MeshFilter meshFilter = MeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = MeshObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = MeshObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = MeshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh.Clear();
        meshFilter.mesh.SetVertices(Vertices.Select(x => x.Position).ToArray()); // Set the vertices
        meshFilter.mesh.SetUVs(0, Vertices.Select(x => x.UV).ToArray()); // Set the UV's
        meshFilter.mesh.SetUVs(1, Vertices.Select(x => x.UV2).ToArray()); // Set the UV's
        meshFilter.mesh.subMeshCount = Triangles.Count; // Set the submesh count
        for (int i = 0; i < Triangles.Count; i++) // Set the triangles for each submesh
        {
            List<int> triangles = new List<int>();
            foreach (MeshTriangle triangle in Triangles[i])
            {
                triangles.Add(triangle.Vertex1.Id);
                triangles.Add(triangle.Vertex2.Id);
                triangles.Add(triangle.Vertex3.Id);
            }
            meshFilter.mesh.SetTriangles(triangles, i);
        }
        meshFilter.mesh.RecalculateNormals();

        if(applyMaterials) meshRenderer.materials = Materials.ToArray(); // Set the material for each submesh

        // Update collider
        GameObject.Destroy(MeshObject.GetComponent<MeshCollider>());
        if(addCollider) MeshObject.AddComponent<MeshCollider>();

        return MeshObject;
    }

    public MeshVertex AddVertex(Vector3 position, Vector2 uv, Vector2? uv2 = null)
    {
        MeshVertex vertex = new MeshVertex(position, uv, uv2);
        Vertices.Add(vertex);
        return vertex;
    }

    public void AddVertex(MeshVertex meshVertex)
    {
        Vertices.Add(meshVertex);
    }

    public void RemoveVertex(MeshVertex meshVertex)
    {
        Vertices.Remove(meshVertex);
    }

    public MeshTriangle AddTriangle(int submeshIndex, MeshVertex vertex1, MeshVertex vertex2, MeshVertex vertex3)
    {
        MeshTriangle triangle = new MeshTriangle(submeshIndex, vertex1, vertex2, vertex3);
        Triangles[submeshIndex].Add(triangle);
        return triangle;
    }
    /// <summary>
    /// Removes a triangle from a submesh. Does not remove the associated vertices. Does not refresh the mesh automatically.
    /// </summary>
    public void RemoveTriangle(MeshTriangle triangle)
    {
        Triangles[triangle.SubmeshIndex].Remove(triangle);
    }

    public int AddNewSubmesh(Material material)
    {
        Triangles.Add(new List<MeshTriangle>());
        CurrentSubmesh++;
        Materials.Add(material);
        return CurrentSubmesh;
    }

    /// <summary>
    /// Adds triangles for a plane to a submesh. Order of vertices must be clockwise
    /// </summary>
    public List<MeshTriangle> AddPlane(int submeshIndex, MeshVertex v1, MeshVertex v2, MeshVertex v3, MeshVertex v4)
    {
        MeshTriangle t1 = AddTriangle(submeshIndex, v1, v3, v2);
        MeshTriangle t2 = AddTriangle(submeshIndex, v1, v4, v3);
        return new List<MeshTriangle>() { t1, t2 };
    }

    /// <summary>
    /// Removes all triangles and vertices of a plane from a submesh. Does not refresh the mesh automatically.
    /// </summary>
    public void RemovePlane(int submeshIndex, MeshPlane plane)
    {
        Vertices.Remove(plane.Vertex1);
        Vertices.Remove(plane.Vertex2);
        Vertices.Remove(plane.Vertex3);
        Vertices.Remove(plane.Vertex4);
        RemoveTriangle(plane.Triangle1);
        RemoveTriangle(plane.Triangle2);
    }

    #region Build Functions

    #endregion
    /// <summary>
    /// Adds all meshvertices and meshtriangles to build a wall. Returns a MeshPlane containing all data.
    /// UV from first to second vector is uv-y-axis
    /// </summary>
    public MeshPlane BuildPlane(int submeshIndex, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector2 uvStart, Vector2 uvEnd, bool mirror = false)
    {
        MeshVertex mv1 = AddVertex(v1, uvStart);
        MeshVertex mv2 = AddVertex(v2, new Vector2(uvStart.x, uvEnd.y));
        MeshVertex mv3 = AddVertex(v3, uvEnd);
        MeshVertex mv4 = AddVertex(v4, new Vector2(uvEnd.x, uvStart.y));

        if(mirror)
        {
            MeshTriangle tri1 = AddTriangle(submeshIndex, mv1, mv2, mv3);
            MeshTriangle tri2 = AddTriangle(submeshIndex, mv1, mv3, mv4);
            return new MeshPlane(mv1, mv2, mv3, mv4, tri1, tri2);
        }
        else
        {
            MeshTriangle tri1 = AddTriangle(submeshIndex, mv1, mv3, mv2);
            MeshTriangle tri2 = AddTriangle(submeshIndex, mv1, mv4, mv3);
            return new MeshPlane(mv1, mv2, mv3, mv4, tri1, tri2);
        }

        
    }

    #region Complex Functions

    /// <summary>
    /// Carves a hole into a plane. Only works correctly for rectangular planes at the moment. The hole position is the center.
    /// </summary>
    public void CarveHoleInPlane(int submeshIndex, MeshPlane plane, Vector2 holePosition, Vector2 holeDimensions)
    {
        // Remove the wall that contains the hole
        RemovePlane(submeshIndex, plane);

        // Add new vertices on the sides of the hole
        Vector3 planeVectorX = plane.Vertex4.Position - plane.Vertex1.Position;
        float planeLengthX = planeVectorX.magnitude;
        float relHoleWidth = holeDimensions.x / planeLengthX;
        float relativeHolePositionX = holePosition.x / planeLengthX;
        float xStart = relativeHolePositionX - relHoleWidth / 2;
        float xEnd = relativeHolePositionX + relHoleWidth / 2;

        Vector3 planeVectorY = plane.Vertex2.Position - plane.Vertex1.Position;
        float planeLengthY = planeVectorY.magnitude;
        float relHoleHeight = holeDimensions.y / planeLengthY;
        float relativeHolePositionY = holePosition.y / planeLengthY;
        float yStart = relativeHolePositionY - relHoleHeight / 2;
        float yEnd = relativeHolePositionY + relHoleHeight / 2;

        float uvVectorX = plane.Vertex4.UV.x - plane.Vertex1.UV.x;
        float uvStartX = plane.Vertex1.UV.x + xStart * uvVectorX;
        float uvEndX = plane.Vertex1.UV.x + xEnd * uvVectorX;

        float uvVectorY = plane.Vertex2.UV.y - plane.Vertex1.UV.y;
        float uvStartY = plane.Vertex1.UV.y + yStart * uvVectorY;
        float uvEndY = plane.Vertex1.UV.y + yEnd * uvVectorY;

        Vector3 pv1 = plane.Vertex1.Position;
        Vector3 pv2 = plane.Vertex2.Position;
        Vector3 pv3 = plane.Vertex3.Position;
        Vector3 pv4 = plane.Vertex4.Position;

        Vector3 sb1 = plane.Vertex1.Position + xStart * planeVectorX;
        Vector3 st1 = plane.Vertex2.Position + xStart * planeVectorX;
        Vector3 st2 = plane.Vertex2.Position + xEnd * planeVectorX;
        Vector3 sb2 = plane.Vertex1.Position + xEnd * planeVectorX;

        Vector3 hb1 = plane.Vertex1.Position + xStart * planeVectorX + yStart * planeVectorY;
        Vector3 ht1 = plane.Vertex1.Position + xStart * planeVectorX + yEnd * planeVectorY;
        Vector3 ht2 = plane.Vertex1.Position + xEnd * planeVectorX + yEnd * planeVectorY;
        Vector3 hb2 = plane.Vertex1.Position + xEnd * planeVectorX + yStart * planeVectorY;

        BuildPlane(submeshIndex, pv1, pv2, st1, sb1, plane.Vertex1.UV, new Vector2(uvStartX, plane.Vertex2.UV.y));
        BuildPlane(submeshIndex, sb2, st2, pv3, pv4, new Vector2(uvEndX, plane.Vertex1.UV.y), plane.Vertex3.UV);

        if (holePosition.y + holeDimensions.y / 2 < planeLengthY) // Add a plane above the hole if one is needed
            BuildPlane(submeshIndex, ht1, st1, st2, ht2, new Vector2(uvStartX, uvEndY), new Vector2(uvEndX, plane.Vertex3.UV.y));

        // Add vertices below the hole
        if (holePosition.y - holeDimensions.y / 2 > 0) // Add a plane below the hole if one is needed
            BuildPlane(submeshIndex, sb1, hb1, hb2, sb2, new Vector2(uvStartX, plane.Vertex1.UV.y), new Vector2(uvEndX, uvStartY));

        ApplyMesh();
    }

    #endregion


}
