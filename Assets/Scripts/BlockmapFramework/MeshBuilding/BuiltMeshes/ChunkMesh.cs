using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents one object/mesh within a chunk
    /// </summary>
    public abstract class ChunkMesh : MonoBehaviour
    {
        protected World World { get; private set; }
        protected Chunk Chunk { get; private set; }

        protected MeshBuilder MeshBuilder;

        protected void OnInit(Chunk chunk)
        {
            Chunk = chunk;
            World = chunk.World;
            transform.SetParent(chunk.transform);
            transform.localPosition = Vector3.zero;
        }

        public void Draw()
        {
            MeshBuilder = new MeshBuilder(gameObject);

            OnDraw();

            MeshBuilder.ApplyMesh();

            // Set chunk values for all materials
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            }

            OnMeshApplied();
        }
        public virtual void OnDraw() { }
        public virtual void OnMeshApplied() { }
        public abstract void SetVisibility(Actor player);

        public void ShowTextures(bool show)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_UseTextures", show ? 1 : 0);
        }
        public void ShowGrid(bool show)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_ShowGrid", show ? 1 : 0);
        }

        public void ShowOverlay(bool show)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_ShowTileOverlay", show ? 1 : 0);
        }
        public void ShowOverlay(Vector2Int localCoordinates, Texture2D texture, Color color, int size)
        {
            ShowOverlay(true);

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetTexture("_TileOverlayTex", texture);
                renderer.materials[i].SetFloat("_TileOverlayX", localCoordinates.x);
                renderer.materials[i].SetFloat("_TileOverlayY", localCoordinates.y);
                renderer.materials[i].SetFloat("_TileOverlaySize", size);
                renderer.materials[i].SetColor("_TileOverlayColor", color);
            }

        }
    }
}
