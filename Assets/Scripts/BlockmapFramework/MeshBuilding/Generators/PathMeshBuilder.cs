using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class PathMeshBuilder
    {
        private const float PATH_CURB_WIDTH = 0.1f;
        private const float PATH_CURB_HEIGHT = 0.02f;
        private const float PATH_HEIGHT = 0.05f;

        private static World World;
        private static BlockmapNode Node;
        private static MeshBuilder MeshBuilder;
        private static int PathSubmesh;
        private static int CurbSubmesh;

        private static float BaseWorldHeight;

        /// <summary>
        /// Builds the path mesh on this node according to the node shape. Adds the mesh to an existing MeshBuilder.
        /// </summary>
        public static void BuildPath(BlockmapNode node, MeshBuilder meshBuilder, int pathSubmesh, int pathCurbSubmesh)
        {
            World = node.World;
            Node = node;
            MeshBuilder = meshBuilder;

            PathSubmesh = pathSubmesh;
            CurbSubmesh = pathCurbSubmesh;

            BaseWorldHeight = Node.BaseHeight * World.TILE_HEIGHT;

            // Center plane
            DrawShapePlane(PathSubmesh, PATH_HEIGHT, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH); // top
            DrawShapePlane(PathSubmesh, 0f, 0f, 1f, 0f, 1f, mirror: true); // bottom

            // Edge connections
            DrawEdge(Direction.N, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH, 1f);
            DrawEdge(Direction.S, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH, 0f, PATH_CURB_WIDTH);
            DrawEdge(Direction.E, 1f - PATH_CURB_WIDTH, 1f, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH);
            DrawEdge(Direction.W, 0f, PATH_CURB_WIDTH, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH);

            // Corner connections
            DrawCorner(Direction.NW, 0, PATH_CURB_WIDTH, 1f - PATH_CURB_WIDTH, 1f);
            DrawCorner(Direction.NE, 1f - PATH_CURB_WIDTH, 1f, 1f - PATH_CURB_WIDTH, 1f);
            DrawCorner(Direction.SE, 1f - PATH_CURB_WIDTH, 1f, 0f, PATH_CURB_WIDTH);
            DrawCorner(Direction.SW, 0, PATH_CURB_WIDTH, 0f, PATH_CURB_WIDTH);
        }

        private static void DrawEdge(Direction dir, float xStart, float xEnd, float yStart, float yEnd)
        {
            // Check if a path is connected in that direction
            bool hasPathConnection = HasPathConnection(dir);

            // Draw connection to adjacent path
            if (hasPathConnection) 
            {
                DrawShapePlane(PathSubmesh, PATH_HEIGHT, xStart, xEnd, yStart, yEnd);
            }

            // Draw curb
            else
            {
                DrawShapePlane(CurbSubmesh, PATH_HEIGHT + PATH_CURB_HEIGHT, xStart, xEnd, yStart, yEnd); // curb top
                DrawCurbSides(dir, xStart, xEnd, yStart, yEnd);
            }
        }

        private static void DrawCorner(Direction dir, float xStart, float xEnd, float yStart, float yEnd)
        {
            // Check if a path is connected in that direction
            bool hasPathConnection = (
                HasPathConnection(HelperFunctions.GetNextAnticlockwiseDirection8(dir)) &&
                HasPathConnection(dir) &&
                HasPathConnection(HelperFunctions.GetNextClockwiseDirection8(dir)));

            // Draw connection to adjacent path corner
            if (hasPathConnection)
            {
                DrawShapePlane(PathSubmesh, PATH_HEIGHT, xStart, xEnd, yStart, yEnd);
            }

            else // Draw curb
            {
                DrawShapePlane(CurbSubmesh, PATH_HEIGHT + PATH_CURB_HEIGHT, xStart, xEnd, yStart, yEnd); // curb top
                DrawCurbSides(dir, xStart, xEnd, yStart, yEnd);
            }
        }

        /// <summary>
        /// Checks and returns if a node with a path exists in the given direction with a matching height to the current node.
        /// </summary>
        private static bool HasPathConnection(Direction dir)
        {
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(Node.WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
                if (adjNode.IsPath && Pathfinder.DoAdjacentHeightsMatch(Node, adjNode, dir))
                    return true;
            return false;
        }

        private static void DrawCurbSides(Direction dir, float xStart, float xEnd, float yStart, float yEnd)
        {

            if (dir == Direction.N || dir == Direction.S || dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) // North curb side
            {
                MeshBuilder.BuildPlane(CurbSubmesh,
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yEnd), Node.LocalCoordinates.y + yEnd),
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yEnd) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yEnd),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yEnd) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yEnd),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yEnd), Node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f)
                    );
            }

            if (dir == Direction.N || dir == Direction.S || dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) // South curb side
            {
                MeshBuilder.BuildPlane(CurbSubmesh,
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yStart), Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yStart) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yStart) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yStart), Node.LocalCoordinates.y + yStart),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    mirror: true
                    );
            }

            if (dir == Direction.W || dir == Direction.E || dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) // West curb side
            {

                MeshBuilder.BuildPlane(CurbSubmesh,
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yStart), Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yStart) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yEnd) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yEnd),
                    new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yEnd), Node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f)
                    );
            }

            if (dir == Direction.W || dir == Direction.E || dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) // East curb side
            {
                MeshBuilder.BuildPlane(CurbSubmesh,
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yStart), Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yStart) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yStart),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yEnd) + PATH_HEIGHT + PATH_CURB_HEIGHT, Node.LocalCoordinates.y + yEnd),
                    new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yEnd), Node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    mirror: true
                    );
            }
        }

        /// <summary>
        /// Creates a plane parrallel to the shape of this tile covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        private static void DrawShapePlane(int submesh, float height, float xStart, float xEnd, float yStart, float yEnd, bool mirror = false)
        {
            Vector3 v_SW_pos = new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yStart) + height, Node.LocalCoordinates.y + yStart);
            Vector2 v_SW_uv = new Vector2(xStart, yStart);
            MeshVertex v_SW = MeshBuilder.AddVertex(v_SW_pos, v_SW_uv, v_SW_uv);

            Vector3 v_SE_pos = new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yStart) + height, Node.LocalCoordinates.y + yStart);
            Vector2 v_SE_uv = new Vector2(xEnd, yStart);
            MeshVertex v_SE = MeshBuilder.AddVertex(v_SE_pos, v_SE_uv, v_SE_uv);

            Vector3 v_NE_pos = new Vector3(Node.LocalCoordinates.x + xEnd, GetVertexHeight(xEnd, yEnd) + height, Node.LocalCoordinates.y + yEnd);
            Vector2 v_NE_uv = new Vector2(xEnd, yEnd);
            MeshVertex v_NE = MeshBuilder.AddVertex(v_NE_pos, v_NE_uv, v_NE_uv);

            Vector3 v_NW_pos = new Vector3(Node.LocalCoordinates.x + xStart, GetVertexHeight(xStart, yEnd) + height, Node.LocalCoordinates.y + yEnd);
            Vector2 v_NW_uv = new Vector2(xStart, yEnd);
            MeshVertex v_NW = MeshBuilder.AddVertex(v_NW_pos, v_NW_uv, v_NW_uv);

            if(mirror) MeshBuilder.AddPlane(submesh, v_SW, v_NW, v_NE, v_SE);
            else MeshBuilder.AddPlane(submesh, v_SW, v_SE, v_NE, v_NW);
        }

        /// <summary>
        /// Returns the world height for the current node at the given relative position.
        /// </summary>
        private static float GetVertexHeight(float x, float y)
        {
            return Node.GetWorldHeightAt(new Vector2(x, y));
        }

    }
}
