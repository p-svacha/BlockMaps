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

        protected MeshBuilder MeshBuilder;
        public MeshRenderer Renderer;
        private bool[] ShowMultiOverlay;

        protected void OnInit(Chunk chunk)
        {
            Chunk = chunk;
            World = chunk.World;
            transform.SetParent(chunk.transform);
            transform.localPosition = Vector3.zero;

            ShowMultiOverlay = new bool[256];
        }

        public void Draw()
        {
            MeshBuilder = new MeshBuilder(gameObject);

            OnDraw();

            MeshBuilder.ApplyMesh();
            Renderer = GetComponent<MeshRenderer>();

            // Set chunk values for all materials
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                Renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                Renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            }

            OnMeshApplied();
        }
        public virtual void OnDraw() { }
        public virtual void OnMeshApplied() { }
        public abstract void SetVisibility(Actor player);

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

        public void SetMultiOverlayTexture(Texture2D texture, Color color)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetTexture("_MultiOverlayTex", texture);
                Renderer.materials[i].SetColor("_MultiOverlayColor", color);
            }
        }
        public void ShowMultiOverlayOnNode(Vector2Int localCoordinates)
        {
            ShowMultiOverlay[localCoordinates.x * 16 + localCoordinates.y] = true;

            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloatArray("_ShowMultiOverlay", ShowMultiOverlay.Select(b => b ? 1.0f : 0.0f).ToArray());
        }
        public void HideMultiOverlayOnNode(Vector2Int localCoordinates)
        {
            ShowMultiOverlay[localCoordinates.x * 16 + localCoordinates.y] = false;

            for (int i = 0; i < Renderer.materials.Length; i++)
                Renderer.materials[i].SetFloatArray("_ShowMultiOverlay", ShowMultiOverlay.Select(b => b ? 1.0f : 0.0f).ToArray());
        }

        public void ShowZoneBorders(float[] zoneBorderArray, float[] zoneBorderColors)
        {
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloatArray("_ZoneBorders", zoneBorderArray);
                Renderer.materials[i].SetFloatArray("_ZoneBorderColors", zoneBorderColors);
            }
        }

        protected void SetShaderVisibilityData(List<float> surfaceVisibilityArray)
        {
            // Set visibility in all surface mesh materials
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetColorArray("_PlayerColors", World.GetAllActors().Select(x => x.Color).ToArray());
                Renderer.materials[i].SetFloat("_FullVisibility", 0);
                Renderer.materials[i].SetFloatArray("_TileVisibility", surfaceVisibilityArray);
            }
        }
    }
}
