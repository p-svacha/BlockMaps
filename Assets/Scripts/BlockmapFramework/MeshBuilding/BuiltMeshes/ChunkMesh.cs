using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents one mesh within a chunk that is used to render multiple instances of the same object type in one object. (i.e. nodes, walls, procedural entities)
    /// </summary>
    public abstract class ChunkMesh : MonoBehaviour
    {
        protected World World { get; private set; }
        protected Chunk Chunk { get; private set; }

        /// <summary>
        /// Used for drawing multiple objects with the same material but different properties in a performant way.
        /// </summary>
        private MaterialPropertyBlock MaterialPropertyBlock;

        public MeshRenderer Renderer { get; private set; }
        private float[] MultiOverlayColorIndices;

        protected void OnInit(Chunk chunk)
        {
            Chunk = chunk;
            World = chunk.World;
            transform.SetParent(chunk.ChunkObject.transform);
            transform.localPosition = Vector3.zero;

            MultiOverlayColorIndices = new float[256];
            for (int i = 0; i < MultiOverlayColorIndices.Length; i++) MultiOverlayColorIndices[i] = -1;
        }

        public virtual void OnMeshApplied()
        {
            Renderer = GetComponent<MeshRenderer>();
            if (MaterialPropertyBlock == null) MaterialPropertyBlock = new MaterialPropertyBlock();
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            SetChunkShaderValues();
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        public abstract void SetVisibility(Actor activeVisionActor);

        public void ShowTextures(bool show)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloat("_UseTextures", show ? 1 : 0);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        public void ShowGrid(bool show)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloat("_ShowGrid", show ? 1 : 0);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        public void ShowTileBlending(bool show)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloat("_BlendThreshhold", show ? 0.4f : 0);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void ShowOverlay(bool show)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloat("_ShowTileOverlay", show ? 1 : 0);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        public void ShowZoneBorders(float[] zoneBorderArray, float[] zoneBorderColors)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloatArray("_ZoneBorders", zoneBorderArray);
            MaterialPropertyBlock.SetFloatArray("_ZoneBorderColors", zoneBorderColors);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void ShowOverlay(Vector2Int localCoordinates, Texture2D texture, Color color, int size)
        {
            ShowOverlay(true);

            Renderer.GetPropertyBlock(MaterialPropertyBlock);

            MaterialPropertyBlock.SetTexture("_TileOverlayTex", texture);
            MaterialPropertyBlock.SetFloat("_TileOverlayX", localCoordinates.x);
            MaterialPropertyBlock.SetFloat("_TileOverlayY", localCoordinates.y);
            MaterialPropertyBlock.SetFloat("_TileOverlaySize", size);
            MaterialPropertyBlock.SetColor("_TileOverlayColor", color);

            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void SetMultiOverlayTexture(Texture2D texture)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetTexture("_MultiOverlayTex", texture);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void ShowMultiOverlayOnNode(Vector2Int localCoordinates, MultiOverlayColor color)
        {
            int index = localCoordinates.x * 16 + localCoordinates.y;
            MultiOverlayColorIndices[index] = (int)color;

            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        public void HideMultiOverlayOnNode(Vector2Int localCoordinates)
        {
            MultiOverlayColorIndices[localCoordinates.x * 16 + localCoordinates.y] = -1;

            Renderer.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);
            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        /// <summary>
        /// Passes all static (and initial) data from the chunk to all shaders of this mesh.
        /// </summary>
        public void SetChunkShaderValues()
        {
            List<Color> multiOverlayColors = new List<Color>() { Color.white, Color.green, Color.yellow }; // Needs to match enum MultiOverlayColor

            Renderer.GetPropertyBlock(MaterialPropertyBlock);

            // Static values
            MaterialPropertyBlock.SetFloat("_ChunkSize", Chunk.Size);
            MaterialPropertyBlock.SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
            MaterialPropertyBlock.SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            MaterialPropertyBlock.SetVectorArray("_MultiOverlayColors", multiOverlayColors.Select(x => HelperFunctions.ColorToVec4(x)).ToArray()); 

            // Initial values
            MaterialPropertyBlock.SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);

            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        protected void SetShaderVisibilityData(List<float> visibilityArray)
        {
            Renderer.GetPropertyBlock(MaterialPropertyBlock);

            MaterialPropertyBlock.SetVectorArray("_PlayerColors", World.GetAllActors().Select(x => HelperFunctions.ColorToVec4(x.Color)).ToArray());
            MaterialPropertyBlock.SetFloat("_FullVisibility", 0);
            MaterialPropertyBlock.SetFloatArray("_TileVisibility", visibilityArray);

            Renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
    }

    public enum MultiOverlayColor
    {
        White = 0,
        Green = 1,
        Yellow = 2
    }
}
