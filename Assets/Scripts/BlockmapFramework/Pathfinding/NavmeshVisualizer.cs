using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class NavmeshVisualizer : MonoBehaviour
    {
        private const float NODE_SIZE = 0.2f;
        private const float TRANSITION_WIDTH = 0.01f;
        private const float TRANSITION_OFFSET = 0.03f;
        public const float TRANSITION_Y_OFFSET = 0.2f;

        private Material SurfaceNodeMat;
        private Material AirNodeMat;
        private Material WaterNodeMat;
        private Material ImpassableNodeMat;

        private Material WalkTransitionMat;
        private Material SingleClimbTransitionMat;
        private Material DoubleClimbTransitionMat;

        private void Awake()
        {
            GameObject navmeshObj = GameObject.Find("NavmeshVisualizer");
            if(navmeshObj != null) Singleton = navmeshObj.GetComponent<NavmeshVisualizer>();
        }

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
            ImpassableNodeMat = new Material(Shader.Find("Sprites/Default"));
            SurfaceNodeMat.color = Color.green;
            AirNodeMat.color = Color.gray;
            WaterNodeMat.color = Color.blue;
            ImpassableNodeMat.color = Color.red;

            WalkTransitionMat = new Material(Shader.Find("Sprites/Default"));
            SingleClimbTransitionMat = new Material(Shader.Find("Sprites/Default"));
            DoubleClimbTransitionMat = new Material(Shader.Find("Sprites/Default"));
            WalkTransitionMat.color = Color.white;
            SingleClimbTransitionMat.color = Color.cyan;
            DoubleClimbTransitionMat.color = Color.blue;

            Vector3 nodeDimensions = new Vector3(NODE_SIZE, NODE_SIZE, NODE_SIZE);

            foreach (Chunk chunk in world.Chunks.Values)
            {
                // Generate line mesh (all transition lines in 1 mesh per chunk)
                GameObject lineObject = new GameObject("transitions");
                lineObject.transform.SetParent(transform);
                MeshBuilder transitionMeshBuilder = new MeshBuilder(lineObject);

                // Generate node mesh (all nodes in 1 mesh per chunk)
                GameObject nodeObject = new GameObject("nodes");
                nodeObject.transform.SetParent(transform);
                MeshBuilder nodeMeshBuilder = new MeshBuilder(nodeObject);

                foreach (BlockmapNode node in chunk.GetAllNodes())
                {
                    Vector3 nodePos = node.GetCenterWorldPosition() - new Vector3(NODE_SIZE / 2f, NODE_SIZE / 2f, NODE_SIZE / 2f);
                    int nodeSubmesh = nodeMeshBuilder.GetSubmesh(GetNodeSubmeshMaterial(node));
                    nodeMeshBuilder.BuildCube(nodeSubmesh, nodePos, nodeDimensions);

                    foreach (Transition t in node.Transitions.Values)
                    {
                        if (entity != null && !t.CanPass(entity)) continue;

                        int transitionSubmesh = transitionMeshBuilder.GetSubmesh(GetTransitionSubmeshMaterial(t));
                        float width = t.GetMovementCost(entity) * TRANSITION_WIDTH;
                        List<Vector3> linePath = t.GetPreviewPath();

                        // Adapt last point (to differentiate where it starts (within the node) and where it ends (before the node))
                        Vector3 last = linePath[linePath.Count - 1];
                        Vector3 secondLast = linePath[linePath.Count - 2];
                        Vector3 dir = last - secondLast;
                        last = secondLast + 0.85f * dir;
                        linePath[linePath.Count - 1] = last;

                        // Add offset
                        Vector3 offset = GetOffset(t, width);
                        for (int i = 0; i < linePath.Count; i++) linePath[i] += offset;

                        // Add to line list
                        List<PathLine> line = new List<PathLine>();
                        for (int i = 0; i < linePath.Count; i++)
                        {
                            line.Add(new PathLine(linePath[i], HelperFunctions.GetDirectionAngle(t.Direction), width));
                        }
                        transitionMeshBuilder.BuildPath(transitionSubmesh, new Path(line));
                    }
                }

                // Render line mesh
                transitionMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);

                // Node mesh
                nodeMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
            }
        }

        private Material GetNodeSubmeshMaterial(BlockmapNode node)
        {
            if (!node.IsPassable()) return ImpassableNodeMat;
            return node.Type switch
            {
                NodeType.Ground => SurfaceNodeMat,
                NodeType.Air => AirNodeMat,
                NodeType.Water => WaterNodeMat,
                _ => throw new System.Exception("node type " + node.Type.ToString() + " not handled.")
            };
        }

        private Material GetTransitionSubmeshMaterial(Transition t)
        {
            if (t is AdjacentWalkTransition) return WalkTransitionMat;
            if (t is SingleClimbTransition) return SingleClimbTransitionMat;
            if (t is DoubleClimbTransition) return DoubleClimbTransitionMat;
            throw new System.Exception("Transition type " + t.GetType().ToString() + " not handled.");
        }

        private Vector3 GetOffset(Transition t, float width)
        {
            float offset = TRANSITION_OFFSET + width;
            return t.Direction switch
            {
                Direction.N => new Vector3(offset, TRANSITION_Y_OFFSET, 0f),
                Direction.NE => new Vector3(offset, TRANSITION_Y_OFFSET, 0f),
                Direction.E => new Vector3(0f, TRANSITION_Y_OFFSET, offset),
                Direction.SE => new Vector3(0f, TRANSITION_Y_OFFSET, -offset),
                Direction.S => new Vector3(-offset, TRANSITION_Y_OFFSET, 0f),
                Direction.SW => new Vector3(0f, TRANSITION_Y_OFFSET, offset),
                Direction.W => new Vector3(0f, TRANSITION_Y_OFFSET, -offset),
                Direction.NW => new Vector3(offset, TRANSITION_Y_OFFSET, 0f),
                _ => throw new System.Exception("Direction " + t.Direction.ToString() + " not handled.")
            };
        }

        public void ClearVisualization()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);
        }

        public static NavmeshVisualizer Singleton;
    }
}
