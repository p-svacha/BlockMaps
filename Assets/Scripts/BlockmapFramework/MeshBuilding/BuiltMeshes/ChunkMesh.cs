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
            SetChunkShaderValues();
        }
        public abstract void SetVisibility(Actor activeVisionActor);

        public void ShowTextures(bool show)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloat("_UseTextures", show ? 1 : 0);
        }
        public void ShowGrid(bool show)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloat("_ShowGrid", show ? 1 : 0);
        }
        public void ShowTileBlending(bool show)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloat("_BlendThreshhold", show ? 0.4f : 0);
        }

        public void ShowOverlay(bool show)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloat("_ShowTileOverlay", show ? 1 : 0);
        }
        public void ShowOverlay(Vector2Int localCoordinates, Texture2D texture, Color color, int size)
        {
            ShowOverlay(true);

            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetTexture("_TileOverlayTex", texture);
                Renderer.materials[i].SetFloat("_TileOverlayX", localCoordinates.x);
                Renderer.materials[i].SetFloat("_TileOverlayY", localCoordinates.y);
                Renderer.materials[i].SetFloat("_TileOverlaySize", size);
                Renderer.materials[i].SetColor("_TileOverlayColor", color);
            }
        }

        public void SetMultiOverlayTexture(Texture2D texture)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetTexture("_MultiOverlayTex", texture);
            }
        }
        public void ShowMultiOverlayOnNode(Vector2Int localCoordinates, MultiOverlayColor color)
        {
            int index = localCoordinates.x * 16 + localCoordinates.y;
            MultiOverlayColorIndices[index] = (int)color;

            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);
            }
        }
        public void HideMultiOverlayOnNode(Vector2Int localCoordinates)
        {
            MultiOverlayColorIndices[localCoordinates.x * 16 + localCoordinates.y] = -1;

            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);
            }
        }

        public void ShowZoneBorders(float[] zoneBorderArray, float[] zoneBorderColors)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloatArray("_ZoneBorders", zoneBorderArray);
                Renderer.materials[i].SetFloatArray("_ZoneBorderColors", zoneBorderColors);
            }
        }

        /// <summary>
        /// Passes all static (and initial) data from the chunk to all shaders of this mesh.
        /// </summary>
        public void SetChunkShaderValues()
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                // Static values
                Renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                Renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                Renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
                Renderer.materials[i].SetColorArray("_MultiOverlayColors", new List<Color>() { Color.white, Color.green, Color.yellow }); // Needs to match enum MultiOverlayColor

                // Initial values
                Renderer.materials[i].SetFloatArray("_MultiOverlayColorIndices", MultiOverlayColorIndices);
            }
        }
        protected void SetShaderVisibilityData(List<float> visibilityArray)
        {
            // Set visibility in all surface mesh materials
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetColorArray("_PlayerColors", World.GetAllActors().Select(x => x.Color).ToArray());
                Renderer.materials[i].SetFloat("_FullVisibility", 0);
                Renderer.materials[i].SetFloatArray("_TileVisibility", visibilityArray);
            }
        }
    }

    public enum MultiOverlayColor
    {
        White = 0,
        Green = 1,
        Yellow = 2
    }
}
