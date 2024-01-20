using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class NavmeshVisualizer : MonoBehaviour
    {
        private const float NODE_SIZE = 0.2f;
        private const float TRANSITION_OFFSET = 0.05f;
        private const float TRANSITION_Y_OFFSET = 0.2f;

        private Material SurfaceNodeMat;
        private Material AirNodeMat;
        private Material WaterNodeMat;

        private Material TransitionMat;

        /// <summary>
        /// Shows the navmesh of the world.
        /// <br/> If an entity is provided the navmesh will be visualized for that entity.
        /// </summary>
        public void Visualize(World world, MovingEntity entity = null)
        {
            ClearVisualization();

            SurfaceNodeMat = new Material(Shader.Find("Sprites/Default"));
            AirNodeMat = new Material(Shader.Find("Sprites/Default"));
            WaterNodeMat = new Material(Shader.Find("Sprites/Default"));
            SurfaceNodeMat.color = Color.green;
            AirNodeMat.color = Color.gray;
            WaterNodeMat.color = Color.blue;

            TransitionMat = new Material(Shader.Find("Standard"));
            TransitionMat.color = Color.black;

            Vector3 nodeDimensions = new Vector3(NODE_SIZE, NODE_SIZE, NODE_SIZE);

            foreach (Chunk chunk in world.Chunks.Values)
            {
                // Generate line mesh (all transition lines in 1 mesh per chunk)
                GameObject lineObject = new GameObject("transitions");
                lineObject.transform.SetParent(transform);
                MeshFilter meshFilter = lineObject.AddComponent<MeshFilter>();
                List<Vector3> linesToRender = new List<Vector3>();
                List<Color> lineColors = new List<Color>();

                // Generate node mesh (all nodes in 1 mesh per chunk)
                GameObject nodeObject = new GameObject("nodes");
                nodeObject.transform.SetParent(transform);
                MeshBuilder nodeMeshBuilder = new MeshBuilder(nodeObject);

                foreach (BlockmapNode node in chunk.GetAllNodes())
                {
                    Vector3 nodePos = node.GetCenterWorldPosition() - new Vector3(NODE_SIZE / 2f, NODE_SIZE / 2f, NODE_SIZE / 2f);
                    int submesh = GetNodeSubmesh(nodeMeshBuilder, node);
                    nodeMeshBuilder.BuildCube(submesh, nodePos, nodeDimensions);

                    foreach (Transition t in node.Transitions.Values)
                    {
                        if (entity != null && !t.CanPass(entity)) continue;

                        Color lineColor = GetTransitionColor(t); // unused because the performant line rendering method i use doesn't support colors and width
                        List<Vector3> linePath = t.GetPreviewPath();

                        // Adapt last point (to differentiate where it starts (within the node) and where it ends (before the node))
                        Vector3 last = linePath[linePath.Count - 1];
                        Vector3 secondLast = linePath[linePath.Count - 2];
                        Vector3 dir = last - secondLast;
                        last = secondLast + 0.85f * dir;
                        linePath[linePath.Count - 1] = last;

                        // Add offset
                        Vector3 offset = GetOffset(t);
                        for (int i = 0; i < linePath.Count; i++) linePath[i] += offset;

                        // Add to line list
                        for (int i = 0; i < linePath.Count - 1; i++)
                        {
                            linesToRender.Add(linePath[i]);
                            linesToRender.Add(linePath[i + 1]);
                            lineColors.Add(lineColor);
                            lineColors.Add(lineColor);
                        }
                    }
                }

                // Render line mesh
                Mesh mesh = new Mesh();
                meshFilter.mesh = mesh;
                Vector3[] vertices = linesToRender.ToArray();
                int[] indices = new int[vertices.Length];
                for (int i = 0; i < indices.Length; i++) indices[i] = i;
                mesh.vertices = vertices;
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                mesh.SetColors(lineColors.ToArray());
                mesh.RecalculateBounds();
                MeshRenderer renderer = lineObject.AddComponent<MeshRenderer>();
                renderer.material = TransitionMat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                // Node mesh
                nodeMeshBuilder.ApplyMesh();
            }
        }

        private int GetNodeSubmesh(MeshBuilder meshBuilder, BlockmapNode node)
        {
            return node.Type switch
            {
                NodeType.Surface => meshBuilder.GetSubmesh(SurfaceNodeMat),
                NodeType.Air => meshBuilder.GetSubmesh(AirNodeMat),
                NodeType.Water => meshBuilder.GetSubmesh(WaterNodeMat),
                _ => throw new System.Exception("node type " + node.Type.ToString() + " not handled.")
            };
        }

        private Color GetTransitionColor(Transition t)
        {
            if (t is AdjacentWalkTransition) return Color.white;
            if (t is SingleClimbTransition) return Color.cyan;
            if (t is DoubleClimbTransition) return Color.blue;
            throw new System.Exception("Transition type " + t.GetType().ToString() + " not handled.");
        }

        private Vector3 GetOffset(Transition t)
        {
            return t.Direction switch
            {
                Direction.N => new Vector3(TRANSITION_OFFSET, TRANSITION_Y_OFFSET, 0f),
                Direction.NE => new Vector3(TRANSITION_OFFSET, TRANSITION_Y_OFFSET, 0f),
                Direction.E => new Vector3(0f, TRANSITION_Y_OFFSET, TRANSITION_OFFSET),
                Direction.SE => new Vector3(0f, TRANSITION_Y_OFFSET, -TRANSITION_OFFSET),
                Direction.S => new Vector3(-TRANSITION_OFFSET, TRANSITION_Y_OFFSET, 0f),
                Direction.SW => new Vector3(0f, TRANSITION_Y_OFFSET, TRANSITION_OFFSET),
                Direction.W => new Vector3(0f, TRANSITION_Y_OFFSET, -TRANSITION_OFFSET),
                Direction.NW => new Vector3(TRANSITION_OFFSET, TRANSITION_Y_OFFSET, 0f),
                _ => throw new System.Exception("Direction " + t.Direction.ToString() + " not handled.")
            };
        }

        public void ClearVisualization()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);
        }

        public static NavmeshVisualizer Singleton { get { return GameObject.Find("NavmeshVisualizer").GetComponent<NavmeshVisualizer>(); } }
    }
}
