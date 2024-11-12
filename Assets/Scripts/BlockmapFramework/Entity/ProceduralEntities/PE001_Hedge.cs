using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework {
    public class PE001_Hedge : ProceduralEntity
    {
        protected override ProceduralEntityId ProceduralId => ProceduralEntityId.PE001;
        protected override bool PE_BlocksVision => true;
        protected override string PE_Name => "Hedge";

        private const float EDGE_OFFSET = 0.2f;
        private const float BEVEL_HEIGHT = 0.15f;
        private const float BEVEL_WIDTH = 0.1f;

        public override void BuildMesh(MeshBuilder meshBuilder, BlockmapNode node, int height, bool isPreview = false)
        {
            Material mat = isPreview ? MaterialManager.BuildPreviewMaterial : MaterialManager.LoadMaterial("Hedge");
            int submesh = meshBuilder.GetSubmesh(mat);

            float hedgeWidth = 1f - 2 * EDGE_OFFSET;
            float hedgeHeight = height * World.TILE_HEIGHT - 0.05f;

            Dictionary<Direction, bool> hasConnection = new Dictionary<Direction, bool>();
            foreach (Direction dir in HelperFunctions.GetAllDirections8()) hasConnection.Add(dir, node.HasEntityConnection(dir, GetTypeId(height)));

            Dictionary<Direction, float> bevelWidths = new Dictionary<Direction, float>();
            foreach (Direction dir in HelperFunctions.GetSides()) bevelWidths.Add(dir, hasConnection[dir] ? 0f : BEVEL_WIDTH);

            meshBuilder.BuildCubeWithBevelledTop(node, submesh, new Vector3(EDGE_OFFSET, 0f, EDGE_OFFSET), new Vector3(hedgeWidth, hedgeHeight, hedgeWidth), BEVEL_HEIGHT, bevelWidths);

            // Side connections
            foreach (Direction dir in HelperFunctions.GetSides())
            {
                if (hasConnection[dir])
                {
                    Direction nextSide = HelperFunctions.GetNextSideDirection(dir);
                    Direction prevSide = HelperFunctions.GetPreviousSideDirection(dir);
                    Direction nextCorner = HelperFunctions.GetNextDirection8(dir);
                    Direction prevCorner = HelperFunctions.GetPreviousDirection8(dir);
                    Dictionary<Direction, float> bevelWidthsSide = new Dictionary<Direction, float> {
                        { dir, 0f }, { HelperFunctions.GetOppositeDirection(dir), 0f },
                        { nextSide , (hasConnection[nextSide] && hasConnection[nextCorner]) ? 0f : BEVEL_WIDTH },
                        { prevSide, (hasConnection[prevCorner] && hasConnection[prevSide]) ? 0f : BEVEL_WIDTH }
                    };

                    Vector3 pos = dir switch
                    {
                        Direction.N => new Vector3(EDGE_OFFSET, 0f, 1f - EDGE_OFFSET),
                        Direction.E => new Vector3(1f - EDGE_OFFSET, 0f, EDGE_OFFSET),
                        Direction.S => new Vector3(EDGE_OFFSET, 0f, 0f),
                        Direction.W => new Vector3(0f, 0f, EDGE_OFFSET),
                        _ => throw new System.Exception("invalid direction")
                    };
                    Vector3 dim = dir switch
                    {
                        Direction.N => new Vector3(hedgeWidth, hedgeHeight, EDGE_OFFSET),
                        Direction.E => new Vector3(EDGE_OFFSET, hedgeHeight, hedgeWidth),
                        Direction.S => new Vector3(hedgeWidth, hedgeHeight, EDGE_OFFSET),
                        Direction.W => new Vector3(EDGE_OFFSET, hedgeHeight, hedgeWidth),
                        _ => throw new System.Exception("invalid direction")
                    };
                    
                    meshBuilder.BuildCubeWithBevelledTop(node, submesh, pos, dim, BEVEL_HEIGHT, bevelWidthsSide);
                }
            }

            // Edge connections
            foreach(Direction dir in HelperFunctions.GetCorners())
            {
                if(HelperFunctions.GetAffectedDirections(dir).TrueForAll(x => hasConnection[x]))
                {
                    Vector3 pos = dir switch
                    {
                        Direction.NW => new Vector3(0f, 0f, 1f - EDGE_OFFSET),
                        Direction.NE => new Vector3(1f - EDGE_OFFSET, 0f, 1f - EDGE_OFFSET),
                        Direction.SE => new Vector3(1f - EDGE_OFFSET, 0f, 0f),
                        Direction.SW => new Vector3(0f, 0f, 0f),
                        _ => throw new System.Exception("invalid direction")
                    };
                    Vector3 dim = new Vector3(EDGE_OFFSET, hedgeHeight, EDGE_OFFSET);

                    meshBuilder.BuildCube(node, submesh, pos, dim);
                }
            }
        }
    }
}
