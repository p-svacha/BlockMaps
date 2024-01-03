using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class ResourceManager : MonoBehaviour
    {
        private void Awake()
        {
            Singleton = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();
        }

        [Header("Materials")]
        public Material SurfaceMaterial;
        public Material CliffMaterial;
        public Material PathMaterial;
        public Material PathCurbMaterial;
        public Material WaterMaterial;
        public Material WaterPreviewMaterial;

        [Header("Colors")]
        public Color GrassColor;
        public Color SandColor;
        public Color TarmacColor;
        public Color WaterColor;
        public Color SolidWallColor;

        [Header("Terrain Textures")]
        public Texture2D GrassTexture;
        public Texture2D SandTexture;
        public Texture2D TarmacTexture;
        public Texture2D WaterTexture;

        [Header("Sprites")]
        public Sprite SolidWallSprite;

        [Header("Prefabs")]
        public Projector SelectionIndicator;

        public static ResourceManager Singleton;
    }
}
