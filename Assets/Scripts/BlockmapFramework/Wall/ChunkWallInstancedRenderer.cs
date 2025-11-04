using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlockmapFramework
{
    /// <summary>
    /// The renderer that draws all walls in a chunk using GPU instancing.
    /// <br/>Rendered instances are bucketed by (mesh + material + visibility state). Each bucket is drawn in batches of up to 1023 instances. 
    /// <br/>A single wall piece (i.e. windows) can contain multiple instances, which are just additional entries in the relevant bucket.
    /// </summary>
    public class ChunkWallInstancedRenderer : MonoBehaviour
    {
        const int BATCH = 1023;

        private Chunk Chunk;

        // Per material, per state -> matrices
        private readonly Dictionary<(Mesh mesh, Material mat), List<Matrix4x4>> VisibleWalls = new();
        private readonly Dictionary<(Mesh mesh, Material mat), List<Matrix4x4>> ExploredWalls = new();

        // Per material MPB (holds per-chunk arrays & toggles)
        readonly Dictionary<(Mesh mesh, Material mat), MaterialPropertyBlock> MaterialPropertyBlocks = new();

        /// <summary>
        /// Called once on creation.
        /// </summary>
        public void Init(Chunk chunk)
        {
            Chunk = chunk;
            gameObject.layer = chunk.World.Layer_WallMesh;
        }

        // Draw from LateUpdate (or a central renderer pass)
        private void LateUpdate()
        {
            DrawBuckets(VisibleWalls);
            DrawBuckets(ExploredWalls);
        }

        /// <summary>
        /// Draws all buckets in the given dictionary using instanced batch rendering. Needs to be called every frame in LateUpdate.
        /// </summary>
        private void DrawBuckets(Dictionary<(Mesh mesh, Material mat), List<Matrix4x4>> walls)
        {
            foreach (var kvp in walls)
            {
                (Mesh mesh, Material mat) key = kvp.Key;
                List<Matrix4x4> matrices = kvp.Value;
                MaterialPropertyBlock mpb = MaterialPropertyBlocks[key];

                for (int i = 0; i < matrices.Count; i += BATCH)
                {
                    int n = Mathf.Min(BATCH, matrices.Count - i);
                    List<Matrix4x4> slice = matrices.GetRange(i, n);
                    Graphics.DrawMeshInstanced(key.mesh, 0, key.mat, slice, mpb, ShadowCastingMode.On, false, Chunk.World.Layer_WallMesh);
                }
            }
        }

        /// <summary>
        /// Rebuilds all buckets based on the current walls in the chunk and their current visibility.
        /// </summary>
        public void RebuildBuckets(Actor activeVisionActor)
        {
            VisibleWalls.Clear(); ExploredWalls.Clear();

            List<Wall> chunkWalls = Chunk.Walls.Values.SelectMany(w => w).ToList();

            foreach (var w in chunkWalls)
            {
                VisibilityType vis = w.GetVisibility(activeVisionActor);
                if (vis == VisibilityType.Unrevealed) continue;

                List<(Mesh mesh, Material mat, Matrix4x4 mtx)> instances = GetMeshInstancesForWall(w);

                foreach(var (mesh, mat, mtx) in instances)
                {
                    var target = (vis == VisibilityType.Visible) ? VisibleWalls : ExploredWalls;
                    if (!target.TryGetValue((mesh, mat), out var list))
                        target[(mesh, mat)] = list = new List<Matrix4x4>();
                    list.Add(mtx);

                    // Ensure MPB exists for this material
                    if (!MaterialPropertyBlocks.ContainsKey((mesh, mat)))
                    {
                        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

                        // Set static per-chunk values
                        mpb.SetFloat("_ChunkSize", Chunk.Size);
                        mpb.SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                        mpb.SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
                        Color[] playerCols = Chunk.World.GetAllActors().Select(a => a.Color).ToArray();
                        mpb.SetVectorArray("_PlayerColors", playerCols.Select(HelperFunctions.ColorToVec4).ToArray());

                        MaterialPropertyBlocks[(mesh, mat)] = mpb;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of all meshes that need to be instanced to display this wall, including their materials and transformation matrices.
        /// </summary>
        private List<(Mesh, Material, Matrix4x4)> GetMeshInstancesForWall(Wall wall)
        {
            List<(Mesh, Material, Matrix4x4)> result = new List<(Mesh, Material, Matrix4x4)>();

            if (wall.Shape == WallShapeDefOf.Solid)
            {
                Mesh mesh = GPUInstancingMeshDatabase.CubeMesh;
                Material mat = wall.Material.Material;
                Matrix4x4 mtx = BuildWallMatrix(wall);
                result.Add((mesh, mat, mtx));
            }
            if (wall.Shape == WallShapeDefOf.Corner)
            {
                Mesh mesh = GPUInstancingMeshDatabase.CubeMesh;
                Material mat = wall.Material.Material;
                Matrix4x4 mtx = BuildWallMatrix(wall);
                result.Add((mesh, mat, mtx));
            }
            if (wall.Shape == WallShapeDefOf.Slope)
            {
                Mesh mesh = wall.IsMirrored ? GPUInstancingMeshDatabase.SlopeWallMeshMirrored : GPUInstancingMeshDatabase.SlopeWallMesh;
                Material mat = wall.Material.Material;
                Matrix4x4 mtx = BuildWallMatrix(wall);
                result.Add((mesh, mat, mtx));
            }
            if(wall.Shape == WallShapeDefOf.Window)
            {
                // Glass piece
                {
                    Mesh mesh = GPUInstancingMeshDatabase.WindowGlassMesh;
                    Material mat = MaterialManager.LoadMaterial("Materials/NodeMaterials/Glass");

                    Vector3 pos = new Vector3(wall.GlobalCellCoordinates.x + 0.5f, wall.GlobalCellCoordinates.y * World.NodeHeight, wall.GlobalCellCoordinates.z + 0.5f);
                    pos += GetWallMeshOffset(wall);
                    Quaternion rot = Quaternion.Euler(0f, HelperFunctions.GetDirectionAngle(wall.Side) - 180f, 0f);
                    Vector3 scale = Vector3.one;

                    Matrix4x4 mtx = Matrix4x4.TRS(pos, rot, scale);

                    result.Add((mesh, mat, mtx));
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the transformation matrix for a single wall piece, defining where it should be rendered through GPU instancing.
        /// </summary>
        private static Matrix4x4 BuildWallMatrix(Wall w)
        {
            float nh = World.NodeHeight;
            float wallW = w.Shape.Width;

            // position at cell base
            Vector3 pos = new Vector3(w.GlobalCellCoordinates.x + 0.5f, w.GlobalCellCoordinates.y * nh, w.GlobalCellCoordinates.z + 0.5f);

            float rotationAngle;
            Vector3 offset;
            Vector3 scale;

            switch (w.Side)
            {
                case Direction.N: rotationAngle = 0f; offset = new(0f, 0f, 0.5f - wallW * 0.5f); break;
                case Direction.E: rotationAngle = 90f; offset = new(0.5f - wallW * 0.5f, 0f, 0f); break;
                case Direction.S: rotationAngle = 180f; offset = new(0f, 0f, -0.5f + wallW * 0.5f); break;
                case Direction.W: rotationAngle = 270f; offset = new(-0.5f + wallW * 0.5f, 0f, 0f); break;
                case Direction.NE: rotationAngle = 0f; offset = new((0.5f - wallW * 0.5f), 0f, (0.5f - wallW * 0.5f)); break;
                case Direction.SE: rotationAngle = 0f; offset = new((0.5f - wallW * 0.5f), 0f, (-0.5f + wallW * 0.5f)); break;
                case Direction.SW: rotationAngle = 0f; offset = new((-0.5f + wallW * 0.5f), 0f, (-0.5f + wallW * 0.5f)); break;
                case Direction.NW: rotationAngle = 0f; offset = new((-0.5f + wallW * 0.5f), 0f, (0.5f - wallW * 0.5f)); break;
                default: rotationAngle = 0f; offset = Vector3.zero; break;
            }
            pos += offset;

            Quaternion rot = Quaternion.Euler(0f, rotationAngle, 0f);
            if(w.Shape == WallShapeDefOf.Corner) scale = new Vector3(wallW, nh, wallW); // X=wall width, Y=node height, Z=wall width
            else scale = new Vector3(1f, nh, wallW); // X=1, Y=node height, Z=wall width

            return Matrix4x4.TRS(pos, rot, scale);
        }

        private static Vector3 GetWallMeshOffset(Wall w)
        {
            float wallWidth = w.Shape.Width;
            switch (w.Side)
            {
                case Direction.N: return new Vector3(0f, 0f, 0.5f - wallWidth * 0.5f);
                case Direction.E: return new Vector3(0.5f - wallWidth * 0.5f, 0f, 0f);
                case Direction.S: return new Vector3(0f, 0f, -0.5f + wallWidth * 0.5f);
                case Direction.W: return new Vector3(-0.5f + wallWidth * 0.5f, 0f, 0f);
                case Direction.NE: return new Vector3((0.5f - wallWidth * 0.5f), 0f, (0.5f - wallWidth * 0.5f));
                case Direction.SE: return new Vector3((0.5f - wallWidth * 0.5f), 0f, (-0.5f + wallWidth * 0.5f));
                case Direction.SW: return new Vector3((-0.5f + wallWidth * 0.5f), 0f, (-0.5f + wallWidth * 0.5f));
                case Direction.NW: return new Vector3((-0.5f + wallWidth * 0.5f), 0f, (0.5f - wallWidth * 0.5f));
                default: return Vector3.zero;
            }
        }
    }
}
