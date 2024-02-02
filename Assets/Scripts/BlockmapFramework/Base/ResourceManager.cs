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
        public Material CliffMaterial;
        public Material PathMaterial;
        public Material PathCurbMaterial;
        public Material WaterMaterial;
        public Material BrickWallMaterial;
        public Material WoodenFenceMaterial;
        public Material LadderMaterial;

        [Header("Colors")]
        public Color GrassColor;
        public Color SandColor;
        public Color TarmacColor;
        public Color WaterColor;

        [Header("WallType Sprites")]
        public Sprite BrickWallSprite;
        public Sprite WoodenFenceSprite;

        [Header("Textures")]
        public Texture2D GrassTexture;
        public Texture2D SandTexture;
        public Texture2D TarmacTexture;
        public Texture2D WaterTexture;

        [Header("Prefabs")]
        public Projector SelectionIndicator;
    }
}
