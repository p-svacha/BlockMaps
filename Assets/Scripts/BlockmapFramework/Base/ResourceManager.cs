using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Singleton;
        private void Awake()
        {
            Singleton = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();
        }

        [Header("Materials")]
        public Material BuildPreviewMaterial;

        public Material SurfaceMaterial;
        public Material LadderMaterial;

        public Material Mat_Asphalt;
        public Material Mat_BrickWall;
        public Material Mat_Rock;
        public Material Mat_Cobblestone;
        public Material Mat_Concrete;
        public Material Mat_ConcreteDark;
        public Material Mat_Concrete2;
        public Material Mat_Hedge;
        public Material Mat_Water;
        public Material Mat_Wood;

        public List<Material> GetAllNodeSurfaceMaterials() => new List<Material>()
        {
            SurfaceMaterial,
        };

        [Header("Colors")]
        public Color GrassColor;
        public Color SandColor;
        public Color TarmacColor;
        public Color WaterColor;

        [Header("Thumbnails")]
        public Sprite Thumbnail_Brickwall;
        public Sprite Thumbnail_WoodenFence;
        public Sprite Thumbnail_CliffWall;

        public Sprite Thumbnail_Hedge;

        [Header("Textures")]
        public Texture2D GrassTexture;
        public Texture2D SandTexture;
        public Texture2D ConcreteTexture;
        public Texture2D WaterTexture;

        [Header("Prefabs")]
        public Projector SelectionIndicator;
    }
}
