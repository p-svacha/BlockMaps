using BlockmapFramework;
using BlockmapFramework.MeshBuilding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class MeshBuilder
    {
        private GameObject MeshObject;

        public List<MeshVertex> Vertices = new List<MeshVertex>(); // Vertices and uv's are shared across all submeshes
        private List<List<MeshTriangle>> Triangles = new List<List<MeshTriangle>>(); // Each list contains the triangles of one submesh
        public int CurrentSubmesh = -1;
        private Dictionary<Material, int> Submeshes; // Stores the materials for each submesh

        private List<Material> Materials = new List<Material>();

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

            Submeshes = new Dictionary<Material, int>();
        }

        /// <summary>
        /// Create a mesh builder for an existing gameobject.
        /// </summary>
        public MeshBuilder(GameObject meshObject)
        {
            MeshObject = meshObject;

            Submeshes = new Dictionary<Material, int>();
        }

        public GameObject ApplyMesh(bool addCollider = true, bool applyMaterials = true, bool castShadows = true)
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

            if (applyMaterials) meshRenderer.materials = Materials.ToArray(); // Set the material for each submesh
            if (!castShadows) meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Update collider
            GameObject.Destroy(MeshObject.GetComponent<MeshCollider>());
            if (addCollider && Vertices.Count > 0) MeshObject.AddComponent<MeshCollider>();

            return MeshObject;
        }

        public MeshVertex AddVertex(Vector3 position, Vector2 uv, Vector2? uv2 = null)
        {
            if (uv2 == null) uv2 = uv;
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

        public MeshTriangle AddTriangle(int submeshIndex, MeshVertex vertex1, MeshVertex vertex2, MeshVertex vertex3, bool mirror = false)
        {
            MeshTriangle triangle = mirror ? new MeshTriangle(submeshIndex, vertex1, vertex3, vertex2) : new MeshTriangle(submeshIndex, vertex1, vertex2, vertex3);
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

        public int GetSubmesh(string materialSubpath) => GetSubmesh(MaterialManager.LoadMaterial(materialSubpath));
        public int GetSubmesh(Material material)
        {
            if (Submeshes.ContainsKey(material)) return Submeshes[material];
            return AddNewSubmesh(material);
        }
        private int AddNewSubmesh(Material material)
        {
            Triangles.Add(new List<MeshTriangle>());
            CurrentSubmesh++;

            Materials.Add(material);
            Submeshes.Add(material, CurrentSubmesh);

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
        /// Adds all meshvertices and the resulting meshriangle to the meshbuilder. Returns a MeshTriangle containing all data.
        /// <br/> Does not support UVs.
        /// <br/> UV2 is forced to 0.5/0.5 so it doesn't interfere with shader (BlockMap-specific).
        /// </summary>
        public MeshTriangle BuildTriangle(int submeshIndex, Vector3 v1, Vector3 v2, Vector3 v3, bool mirror = false)
        {
            MeshVertex mv1 = AddVertex(v1, Vector2.zero, new Vector2(0.5f, 0.5f));
            MeshVertex mv2 = AddVertex(v2, Vector2.zero, new Vector2(0.5f, 0.5f));
            MeshVertex mv3 = AddVertex(v3, Vector2.zero, new Vector2(0.5f, 0.5f));

            if (mirror) return AddTriangle(submeshIndex, mv1, mv2, mv3);
            else return AddTriangle(submeshIndex, mv1, mv3, mv2);
        }

        /// <summary>
        /// Adds all meshvertices and meshtriangles to build a plane. Returns a MeshPlane containing all data.
        /// UV from first to second vector is uv-x-axis
        /// <br/> UV2 is forced to 0.5/0.5 so it doesn't interfere with shader (BlockMap-specific).
        /// </summary>
        public MeshPlane BuildPlane(int submeshIndex, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector2 uvStart, Vector2 uvEnd, bool mirror = false)
        {
            MeshVertex mv1 = AddVertex(v1, uvStart);
            MeshVertex mv2 = AddVertex(v2, new Vector2(uvEnd.x, uvStart.y));
            MeshVertex mv3 = AddVertex(v3, uvEnd);
            MeshVertex mv4 = AddVertex(v4, new Vector2(uvStart.x, uvEnd.y));

            if (mirror)
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

        /// <summary>
        /// Adds all MeshVertices and MeshTriangles to build a cube.
        /// UV from first to second vector is uv-y-axis
        /// </summary>
        public void BuildCube(int submeshIndex, Vector3 startPos, Vector3 dimensions)
        {
            float xStart = startPos.x;
            float xEnd = startPos.x + dimensions.x;
            float yStart = startPos.y;
            float yEnd = startPos.y + dimensions.y;
            float zStart = startPos.z;
            float zEnd = startPos.z + dimensions.z;

            // bot
            BuildPlane(submeshIndex, new Vector3(xStart, yStart, zStart), new Vector3(xStart, yStart, zEnd), new Vector3(xEnd, yStart, zEnd), new Vector3(xEnd, yStart, zStart), Vector2.zero, Vector2.one);
            // top
            BuildPlane(submeshIndex, new Vector3(xStart, yEnd, zStart), new Vector3(xEnd, yEnd, zStart), new Vector3(xEnd, yEnd, zEnd), new Vector3(xStart, yEnd, zEnd), Vector2.zero, Vector2.one);

            // south
            BuildPlane(submeshIndex, new Vector3(xStart, yStart, zStart), new Vector3(xEnd, yStart, zStart), new Vector3(xEnd, yEnd, zStart), new Vector3(xStart, yEnd, zStart), Vector2.zero, Vector2.one);
            // north
            BuildPlane(submeshIndex, new Vector3(xStart, yStart, zEnd), new Vector3(xStart, yEnd, zEnd), new Vector3(xEnd, yEnd, zEnd), new Vector3(xEnd, yStart, zEnd), Vector2.zero, Vector2.one);

            // west
            BuildPlane(submeshIndex, new Vector3(xStart, yStart, zStart), new Vector3(xStart, yEnd, zStart), new Vector3(xStart, yEnd, zEnd), new Vector3(xStart, yStart, zEnd), Vector2.zero, Vector2.one);
            // east
            BuildPlane(submeshIndex, new Vector3(xEnd, yStart, zStart), new Vector3(xEnd, yStart, zEnd), new Vector3(xEnd, yEnd, zEnd), new Vector3(xEnd, yEnd, zStart), Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Adds all MeshVertices and MeshTriangles to build a cube given a footprint (4 vertices) and a height.
        /// </summary>
        public void BuildCube(int submeshIndex, Vector3 vb1, Vector3 vb2, Vector3 vb3, Vector3 vb4, float height, Vector2? topUvStart = null, Vector2? topUvEnd = null)
        {
            Vector3 vt1 = vb1 + new Vector3(0f, height, 0f);
            Vector3 vt2 = vb2 + new Vector3(0f, height, 0f);
            Vector3 vt3 = vb3 + new Vector3(0f, height, 0f);
            Vector3 vt4 = vb4 + new Vector3(0f, height, 0f);

            // bot
            BuildPlane(submeshIndex, vb4, vb3, vb2, vb1, Vector2.zero, Vector2.one);
            // top
            BuildPlane(submeshIndex, vt1, vt2, vt3, vt4, topUvStart ?? Vector2.zero, topUvEnd ?? Vector2.zero);

            // south
            BuildPlane(submeshIndex, vb1, vb2, vt2, vt1, Vector2.zero, Vector2.one);
            // north
            BuildPlane(submeshIndex, vb3, vb4, vt4, vt3, Vector2.zero, Vector2.one);

            // west
            BuildPlane(submeshIndex, vb2, vb3, vt3, vt2, Vector2.zero, Vector2.one);
            // east
            BuildPlane(submeshIndex, vb4, vb1, vt1, vt4, Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Create a MeshPlane out of 4 MeshVertices that already contain all UV data.
        /// </summary>
        public MeshPlane BuildPlane(int submeshIndex, MeshVertex mv1, MeshVertex mv2, MeshVertex mv3, MeshVertex mv4)
        {
            MeshTriangle tri1 = AddTriangle(submeshIndex, mv1, mv3, mv2);
            MeshTriangle tri2 = AddTriangle(submeshIndex, mv1, mv4, mv3);

            return new MeshPlane(mv1, mv2, mv3, mv4, tri1, tri2);
        }

        /// <summary>
        /// Builds a flat way along a path with a given width.
        /// </summary>
        public void BuildPath(int submeshIndex, Path path, float uvScalingY = 1f)
        {
            float currentUvY = 0f;

            // Create initial two points
            MeshVertex lastL = AddVertex(path.Points[0].Left, new Vector2(0f, currentUvY));
            MeshVertex lastR = AddVertex(path.Points[0].Right, new Vector2(1f, currentUvY));

            for (int i = 1; i < path.Points.Count; i++)
            {
                currentUvY += uvScalingY * Vector3.Distance(new Vector3(path.Points[i].Center.x, 0, path.Points[i].Center.z), new Vector3(path.Points[i - 1].Center.x, 0, path.Points[i - 1].Center.z));

                // Connect current 2 points with last two points.
                MeshVertex nextL = AddVertex(path.Points[i].Left, new Vector2(0f, currentUvY));
                MeshVertex nextR = AddVertex(path.Points[i].Right, new Vector2(1f, currentUvY));

                BuildPlane(submeshIndex, lastL, lastR, nextR, nextL);

                lastL = nextL;
                lastR = nextR;
            }
        }

        #region Complex Functions

        /// <summary>
        /// Carves a hole into a plane. Only works correctly for rectangular planes at the moment. The hole position is the center.
        /// </summary>
        public void CarveHoleInPlane(int submeshIndex, MeshPlane plane, Vector2 holePosition, Vector2 holeDimensions)
        {
            // Remove the plane that contains the hole
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

        #region Blockmap-Specific

        private List<Vector3> GetFootprint(Vector3 position, Vector3 dimensions)
        {
            return new List<Vector3>() {
                new Vector3(position.x, position.y, position.z),
                new Vector3(position.x + dimensions.x, position.y, position.z),
                new Vector3(position.x + dimensions.x, position.y, position.z + dimensions.z),
                new Vector3(position.x, position.y, position.z + dimensions.z),
            };
        }

        /// <summary>
        /// Builds a triangle into a given cell, given the relative position of the 3 vertices within the cell.
        /// <br/>Relative positions are from SW corner and get translated to the given direction.
        /// </summary>
        public void BuildTriangle(Vector3Int localCellPos, Direction side, int submesh, Vector3 p1, Vector3 p2, Vector3 p3, bool mirror = false)
        {
            // Calculate flat footprint vertex positions
            List<Vector3> vertices = new List<Vector3>() { p1, p2, p3 };

            // Translate footprint positions based on direction
            vertices = vertices.Select(x => TranslatePosition(x, side)).ToList();

            // Apply offset based on cell position within the chunk
            Vector3 nodeOffsetPos = new Vector3(localCellPos.x, localCellPos.y * World.NodeHeight, localCellPos.z);
            for (int i = 0; i < vertices.Count; i++) vertices[i] += nodeOffsetPos;

            // Build cube
            BuildTriangle(submesh, vertices[0], vertices[1], vertices[2], mirror);
        }

        /// <summary>
        /// Builds a plane into a given cell, given the relative position of the 4 vertices within the cell.
        /// <br/>Relative positions are from SW corner and get translated to the given direction.
        /// </summary>
        public void BuildPlane(Vector3Int localCellPos, Direction side, int submesh, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, bool mirror = false)
        {
            // Calculate flat footprint vertex positions
            List<Vector3> vertices = new List<Vector3>() { p1, p2, p3, p4 };

            // Translate footprint positions based on direction
            vertices = vertices.Select(x => TranslatePosition(x, side)).ToList();

            // Apply offset based on cell position within the chunk
            Vector3 nodeOffsetPos = new Vector3(localCellPos.x, localCellPos.y * World.NodeHeight, localCellPos.z);
            for (int i = 0; i < vertices.Count; i++) vertices[i] += nodeOffsetPos;

            // Build cube
            BuildPlane(submesh, vertices[0], vertices[1], vertices[2], vertices[3], Vector2.zero, Vector2.one, mirror);
        }

        /// <summary>
        /// Builds a cube into a given cell, given the relative position and dimensions within the cell.
        /// <br/>Relative positions are from SW corner and get translated to the given direction.
        /// </summary>
        public void BuildCube(Vector3Int localCellPos, Direction side, int submesh, Vector3 relativePos, Vector3 relativeDim)
        {
            // Calculate footprint vertex positions
            List<Vector3> footprint = GetFootprint(relativePos, relativeDim);

            // Translate footprint positions based on direction
            footprint = footprint.Select(x => TranslatePosition(x, side)).ToList();

            // Apply offset based on cell position within the chunk
            Vector3 nodeOffsetPos = new Vector3(localCellPos.x, localCellPos.y * World.NodeHeight, localCellPos.z);
            for (int i = 0; i < footprint.Count; i++) footprint[i] += nodeOffsetPos;

            // Build cube
            BuildCube(submesh, footprint[0], footprint[1], footprint[2], footprint[3], relativeDim.y);
        }

        /// <summary>
        /// Builds a cube onto a node, given the relative position and dimensions on that node.
        /// </summary>
        public void BuildCube(BlockmapNode node, int submesh, Vector3 relPos, Vector3 relDim, bool adjustToNodeShape)
        {
            // Calculate flat footprint vertex positions
            List<Vector3> footprint = GetFootprint(relPos, relDim);

            // Apply height offsets from slope to footprint
            if (adjustToNodeShape)
            {
                if (!node.IsFlat())
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float height = node.GetLocalShapeAltitude(new Vector2(footprint[i].x, footprint[i].z)) * World.NodeHeight;
                        footprint[i] += new Vector3(0f, height, 0f);
                    }
                }
            }

            // Apply offset based on node position on chunk to footprint
            float worldHeight = node.BaseWorldAltitude;
            Vector3 nodeOffsetPos = new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y);

            for (int i = 0; i < 4; i++) footprint[i] += nodeOffsetPos;

            // Build cube
            BuildCube(submesh, footprint[0], footprint[1], footprint[2], footprint[3], relDim.y, topUvStart: new Vector2(relPos.x, relPos.z), topUvEnd: new Vector2(relPos.x + relDim.x, relPos.z + relDim.z));
        }

        /// <summary>
        /// Builds a cube with a bevelled top onto a node, given the relative position and dimensions on that node.
        /// </summary>
        public void BuildCubeWithBevelledTop(BlockmapNode node, int submeshIndex, Vector3 relPos, Vector3 relDim, float bevelHeight, Dictionary<Direction, float> bevelSizes)
        {
            // Calculate all vertex positions
            float bottomY = relPos.y;
            float midY = relPos.y + relDim.y - bevelHeight;
            float topY = relPos.y + relDim.y;

            List<Vector3> vertices = new List<Vector3>()
            {
                new Vector3(relPos.x, bottomY, relPos.z), // Bottom SW
                new Vector3(relPos.x + relDim.x, bottomY, relPos.z), // Bottom SE
                new Vector3(relPos.x + relDim.x, bottomY, relPos.z + relDim.z), // Bottom NE
                new Vector3(relPos.x, bottomY, relPos.z + relDim.z), // Bottom NW

                new Vector3(relPos.x, midY, relPos.z), // Mid SW
                new Vector3(relPos.x + relDim.x, midY, relPos.z), // Mid SE
                new Vector3(relPos.x + relDim.x, midY, relPos.z + relDim.z), // Mid NE
                new Vector3(relPos.x, midY, relPos.z + relDim.z), // Mid NW

                new Vector3(relPos.x + bevelSizes[Direction.W], topY, relPos.z + bevelSizes[Direction.S]), // Top SW
                new Vector3(relPos.x + relDim.x - bevelSizes[Direction.E], topY, relPos.z + bevelSizes[Direction.S]), // Top SE
                new Vector3(relPos.x + relDim.x - bevelSizes[Direction.E], topY, relPos.z + relDim.z - bevelSizes[Direction.N]), // Top NE
                new Vector3(relPos.x + bevelSizes[Direction.W], topY, relPos.z + relDim.z - bevelSizes[Direction.N]), // Top NW
            };

            // Apply height offsets from slope to all vertices
            if (!node.IsFlat())
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    float height = node.GetLocalShapeAltitude(new Vector2(vertices[i].x, vertices[i].z)) * World.NodeHeight;
                    vertices[i] += new Vector3(0f, height, 0f);
                }
            }

            // Apply offset based on node position on chunk to footprint
            float worldHeight = node.BaseWorldAltitude;
            Vector3 nodeOffsetPos = new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y);
            for (int i = 0; i < vertices.Count; i++) vertices[i] += nodeOffsetPos;

            // Lower sides
            BuildPlane(submeshIndex, vertices[0], vertices[4], vertices[5], vertices[1], Vector2.zero, Vector2.one, mirror: true); // South
            BuildPlane(submeshIndex, vertices[2], vertices[6], vertices[7], vertices[3], Vector2.zero, Vector2.one, mirror: true); // North
            BuildPlane(submeshIndex, vertices[1], vertices[5], vertices[6], vertices[2], Vector2.zero, Vector2.one, mirror: true); // East
            BuildPlane(submeshIndex, vertices[0], vertices[4], vertices[7], vertices[3], Vector2.zero, Vector2.one); // West

            // Middle/bevel sides
            BuildPlane(submeshIndex, vertices[4], vertices[8], vertices[9], vertices[5], Vector2.zero, Vector2.one, mirror: true); // South
            BuildPlane(submeshIndex, vertices[6], vertices[10], vertices[11], vertices[7], Vector2.zero, Vector2.one, mirror: true); // North
            BuildPlane(submeshIndex, vertices[5], vertices[9], vertices[10], vertices[6], Vector2.zero, Vector2.one, mirror: true); // East
            BuildPlane(submeshIndex, vertices[4], vertices[8], vertices[11], vertices[7], Vector2.zero, Vector2.one); // West

            // Top side
            BuildPlane(submeshIndex, vertices[8], vertices[9], vertices[10], vertices[11], Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Builds a cube onto a node, given the relative position, dimensions and side on that node.
        /// <br/>Relative positions are from SW corner and get translated to the given direction.
        /// </summary>
        public void BuildCube(BlockmapNode node, Direction side, int submesh, Vector3 pos, Vector3 dimensions, bool adjustToNodeSlope)
        {
            List<float> heightOffsets = new List<float>() { 0f, 0f, 0f, 0f };

            // Calculate vertex height offsets if built on a slope
            bool adjustHeightsToSlope = (HelperFunctions.IsSide(side) && adjustToNodeSlope);
            if (adjustHeightsToSlope)
            {
                float slope = World.NodeHeight * (node.Altitude[HelperFunctions.GetPreviousDirection8(side)] - node.Altitude[HelperFunctions.GetNextDirection8(side)]);
                float startY = 0;
                if (slope < 0)
                {
                    startY = -slope;
                    slope = 0;
                }

                float startYOffset = Mathf.Lerp(startY, slope, pos.x);
                float endYOffset = Mathf.Lerp(startY, slope, pos.x + dimensions.x);
                heightOffsets[0] = startYOffset;
                heightOffsets[1] = endYOffset;
                heightOffsets[2] = endYOffset;
                heightOffsets[3] = startYOffset;
            }

            // Calculate footprint vertex positions
            List<Vector3> footprint = GetFootprint(pos, dimensions);

            // Translate footprint positions based on direction
            footprint = footprint.Select(x => TranslatePosition(x, side)).ToList();

            // Apply height offsets from slope
            for (int i = 0; i < 4; i++) footprint[i] += new Vector3(0f, heightOffsets[i], 0f);

            // Apply offset based on node position on chunk to footprint
            int startHeightCoordinate = node.GetMinAltitude(side);
            float worldHeight = node.World.GetWorldHeight(startHeightCoordinate);
            Vector3 nodeOffsetPos = new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y);

            for (int i = 0; i < 4; i++) footprint[i] += nodeOffsetPos;

            // Build cube
            BuildCube(submesh, footprint[0], footprint[1], footprint[2], footprint[3], dimensions.y);
        }

        /// <summary>
        /// Builds a plane onto a node, given the relative position of the 4 vertices and side on that node.
        /// <br/>Relative positions are from S side / SW corner and get translated to the given direction.
        /// </summary>
        public void BuildPlane(BlockmapNode node, Direction side, int submesh, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, bool adjustToNodeSlope, bool mirror = false)
        {
            // Calculate flat footprint vertex positions
            List<Vector3> vertices = new List<Vector3>() { p1, p2, p3, p4 };

            // Translate footprint positions based on direction
            vertices = vertices.Select(x => TranslatePosition(x, side)).ToList();

            // Apply height offsets from slope to footprint
            if (!node.IsFlat() && adjustToNodeSlope)
            {
                for (int i = 0; i < 4; i++)
                {
                    float height = node.GetLocalShapeAltitude(new Vector2(vertices[i].x, vertices[i].z)) * World.NodeHeight;
                    vertices[i] += new Vector3(0f, height, 0f);
                }
            }

            // Apply offset based on node position on chunk to footprint
            int startHeightCoordinate = node.GetMinAltitude(side);
            float worldHeight = node.World.GetWorldHeight(startHeightCoordinate);
            Vector3 nodeOffsetPos = new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y);

            for (int i = 0; i < 4; i++) vertices[i] += nodeOffsetPos;

            // Build plane
            BuildPlane(submesh, vertices[0], vertices[1], vertices[2], vertices[3], Vector2.zero, Vector2.one, mirror);
        }

        public static Vector3 TranslatePosition(Vector3 pos, Direction dir)
        {
            return dir switch
            {
                Direction.S => pos,
                Direction.W => new Vector3(pos.z, pos.y, 1 - pos.x),
                Direction.N => new Vector3(1 - pos.x, pos.y, 1 - pos.z),
                Direction.E => new Vector3(1 - pos.z, pos.y, pos.x),
                Direction.SW => pos,
                Direction.SE => new Vector3(1 - pos.z, pos.y, pos.x),
                Direction.NE => new Vector3(1 - pos.x, pos.y, 1 - pos.z),
                Direction.NW => new Vector3(pos.z, pos.y, 1 - pos.x),
                _ => throw new System.Exception("not handled")
            };
        }

        /// <summary>
        /// Creates a plane parrallel to the surface shape of a node covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        public void DrawShapePlane(BlockmapNode node, string materialSubpath, float height, float xStart, float xEnd, float yStart, float yEnd, bool mirror = false) => DrawShapePlane(node, MaterialManager.LoadMaterial(materialSubpath), height, xStart, xEnd, yStart, yEnd, mirror);

        /// <summary>
        /// Creates a plane parrallel to the surface shape of a node covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        public void DrawShapePlane(BlockmapNode node, Material material, float height, float xStart, float xEnd, float yStart, float yEnd, bool mirror = false) => DrawShapePlane(node, GetSubmesh(material), height, xStart, xEnd, yStart, yEnd, mirror);

        /// <summary>
        /// Creates a plane parrallel to the surface shape of a node covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        public void DrawShapePlane(BlockmapNode node, int submesh, float height, float xStart, float xEnd, float yStart, float yEnd, bool mirror = false)
        {
            Vector3 v_SW_pos = new Vector3(node.LocalCoordinates.x + xStart, node.GetWorldShapeAltitude(new Vector2(xStart, yStart)) + height, node.LocalCoordinates.y + yStart);
            Vector2 v_SW_uv = new Vector2(xStart, yStart);
            MeshVertex v_SW = AddVertex(v_SW_pos, v_SW_uv);

            Vector3 v_SE_pos = new Vector3(node.LocalCoordinates.x + xEnd, node.GetWorldShapeAltitude(new Vector2(xEnd, yStart)) + height, node.LocalCoordinates.y + yStart);
            Vector2 v_SE_uv = new Vector2(xEnd, yStart);
            MeshVertex v_SE = AddVertex(v_SE_pos, v_SE_uv);

            Vector3 v_NE_pos = new Vector3(node.LocalCoordinates.x + xEnd, node.GetWorldShapeAltitude(new Vector2(xEnd, yEnd)) + height, node.LocalCoordinates.y + yEnd);
            Vector2 v_NE_uv = new Vector2(xEnd, yEnd);
            MeshVertex v_NE = AddVertex(v_NE_pos, v_NE_uv);

            Vector3 v_NW_pos = new Vector3(node.LocalCoordinates.x + xStart, node.GetWorldShapeAltitude(new Vector2(xStart, yEnd)) + height, node.LocalCoordinates.y + yEnd);
            Vector2 v_NW_uv = new Vector2(xStart, yEnd);
            MeshVertex v_NW = AddVertex(v_NW_pos, v_NW_uv);

            if (node.GetTriangleMeshShapeVariant())
            {
                AddTriangle(submesh, v_SW, v_NE, v_SE, mirror);
                AddTriangle(submesh, v_SW, v_NW, v_NE, mirror);
            }
            else
            {
                AddTriangle(submesh, v_SW, v_NW, v_SE, mirror);
                AddTriangle(submesh, v_SE, v_NW, v_NE, mirror);
            }
        }

        #endregion

    }
}
