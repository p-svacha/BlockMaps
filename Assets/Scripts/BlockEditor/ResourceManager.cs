using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class ResourceManager : MonoBehaviour
    {
        [Header("Tile Selection Textures")]
        public Texture2D TileSelector;
        public Texture2D TileSelectorN;
        public Texture2D TileSelectorE;
        public Texture2D TileSelectorS;
        public Texture2D TileSelectorW;
        public Texture2D TileSelectorNE;
        public Texture2D TileSelectorNW;
        public Texture2D TileSelectorSW;
        public Texture2D TileSelectorSE;

        [Header("Sprites")]
        public Sprite TerrainToolSprite;
        public Sprite SurfaceToolSprite;
        public Sprite PathToolSprite;
        public Sprite AirNodeSprite;
        public Sprite AirSlopeNodeSprite;
        public Sprite WorldGenSprite;
        public Sprite StaticEntitySprite;
        public Sprite MovingEntitySprite;

        public static ResourceManager Singleton { get { return GameObject.Find("ResourceManager").GetComponent<ResourceManager>(); } }
    }
}
