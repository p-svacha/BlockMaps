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

        [Header("Terrain Textures")]
        public Texture2D GrassTexture;
        public Texture2D SandTexture;
        public Texture2D TarmacTexture;

        [Header("Prefabs")]
        public Projector SelectionIndicator;
        public GameObject WaterPlane;

        public static ResourceManager Singleton;
    }
}
