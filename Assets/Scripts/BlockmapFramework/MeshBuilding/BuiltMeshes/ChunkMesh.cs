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

        protected void OnInit(Chunk chunk)
        {
            Chunk = chunk;
            World = chunk.World;
            transform.SetParent(chunk.transform);
        }

        public abstract void Draw();
        public abstract void SetVisibility(Player player);

        public void ShowTextures(bool show)
        {
            if (!gameObject.activeSelf) return;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_UseTextures", show ? 1 : 0);
        }
        public void ShowGrid(bool show)
        {
            if (!gameObject.activeSelf) return;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_ShowGrid", show ? 1 : 0);
        }

        public void ShowOverlay(bool show)
        {
            if (!gameObject.activeSelf) return;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
                renderer.materials[i].SetFloat("_ShowTileOverlay", show ? 1 : 0);
        }
        public void ShowOverlay(Vector2Int localCoordinates, Texture2D texture, Color color)
        {
            if (!gameObject.activeSelf) return;

            ShowOverlay(true);

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetTexture("_TileOverlayTex", texture);
                renderer.materials[i].SetFloat("_TileOverlayX", localCoordinates.x);
                renderer.materials[i].SetFloat("_TileOverlayY", localCoordinates.y);
                renderer.materials[i].SetColor("_TileOverlayColor", color);
            }

        }
    }
}
