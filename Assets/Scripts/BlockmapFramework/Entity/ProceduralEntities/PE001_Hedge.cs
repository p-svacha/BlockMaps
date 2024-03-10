using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework {
    public class PE001_Hedge : ProceduralEntity
    {
        public const string TYPE_ID = "PE001_Hedge";
        private const float HEDGE_EDGE_OFFSET = 0.2f;

        public override Texture2D GetEditorThumbnail() => new Texture2D(100, 100);

        public PE001_Hedge()
        {
            TypeId = TYPE_ID;
            Name = "Hedge";
            Dimensions = new Vector3Int(1, 1, 1);
        }

        public override void BuildMesh(MeshBuilder meshBuilder, BlockmapNode node, bool isPreview = false)
        {
            World world = node.World;

            Material mat = isPreview ? ResourceManager.Singleton.BuildPreviewMaterial : ResourceManager.Singleton.Mat_Hedge;
            int hedgeSubmesh = meshBuilder.GetSubmesh(mat);
            meshBuilder.BuildCube(node, hedgeSubmesh, new Vector3(HEDGE_EDGE_OFFSET, 0f, HEDGE_EDGE_OFFSET), new Vector3(1f - 2 * HEDGE_EDGE_OFFSET, World.TILE_HEIGHT, 1f - 2 * HEDGE_EDGE_OFFSET));

            if(node.HasEntityConnection(Direction.N, TYPE_ID)) // Connection north
                meshBuilder.BuildCube(node, hedgeSubmesh, new Vector3(HEDGE_EDGE_OFFSET, 0f, 1f - HEDGE_EDGE_OFFSET), new Vector3(1f - 2 * HEDGE_EDGE_OFFSET, World.TILE_HEIGHT, HEDGE_EDGE_OFFSET));

            if (node.HasEntityConnection(Direction.S, TYPE_ID)) // Connection south
                meshBuilder.BuildCube(node, hedgeSubmesh, new Vector3(HEDGE_EDGE_OFFSET, 0f, 0f), new Vector3(1f - 2 * HEDGE_EDGE_OFFSET, World.TILE_HEIGHT, HEDGE_EDGE_OFFSET));
        }
    }
}
