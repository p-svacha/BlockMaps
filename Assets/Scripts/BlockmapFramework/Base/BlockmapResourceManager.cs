using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class BlockmapResourceManager : MonoBehaviour
    {
        public Material DefaultMaterial;
        public Material GrassMaterial;
        public Material RockMaterial;

        public Texture2D TileSelector;
        public Texture2D TileSelectorN;
        public Texture2D TileSelectorE;
        public Texture2D TileSelectorS;
        public Texture2D TileSelectorW;
        public Texture2D TileSelectorNE;
        public Texture2D TileSelectorNW;
        public Texture2D TileSelectorSW;
        public Texture2D TileSelectorSE;

        public static BlockmapResourceManager Singleton { get { return GameObject.Find("ResourceManager").GetComponent<BlockmapResourceManager>(); } }
    }
}
