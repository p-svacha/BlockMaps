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
        public Sprite DynamicEntitySprite;

        public Sprite TerrainToolSprite;
        public Sprite SurfaceToolSprite;
        public Sprite AirNodeSprite;
        public Sprite AirSlopeNodeSprite;
        public Sprite WorldGenSprite;
        public Sprite StaticEntitySprite;
        public Sprite ProceduralEntitySprite;
        public Sprite MovingEntitySprite;
        public Sprite MoveEntityToolSprite;
        public Sprite WaterToolSprite;
        public Sprite WallToolSprite;
        public Sprite LadderToolSprite;

        [Header("Thumbnails")]
        public Sprite Thumbnail_Brickwall;
        public Sprite Thumbnail_WoodenFence;

        public Sprite Thumbnail_Hedge;

        public Texture2D GetTileSelector(Direction dir)
        {
            if (dir == Direction.None) return TileSelector;
            if (dir == Direction.N) return TileSelectorN;
            if (dir == Direction.E) return TileSelectorE;
            if (dir == Direction.S) return TileSelectorS;
            if (dir == Direction.W) return TileSelectorW;
            if (dir == Direction.NE) return TileSelectorNE;
            if (dir == Direction.NW) return TileSelectorNW;
            if (dir == Direction.SW) return TileSelectorSW;
            if (dir == Direction.SE) return TileSelectorSE;
            return null;
        }

        public static ResourceManager Singleton { get { return GameObject.Find("ResourceManager").GetComponent<ResourceManager>(); } }
    }
}
