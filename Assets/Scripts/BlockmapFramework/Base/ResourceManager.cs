using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Singleton;

        private static string MATERIALS_BASE_PATH = "BlockmapFramework/Materials/";
        private void Awake()
        {
            Singleton = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();

            BuildPreviewMaterial = Resources.Load<Material>(MATERIALS_BASE_PATH + "Special/BuildPreviewMaterial");
            SurfaceMaterial = Resources.Load<Material>(MATERIALS_BASE_PATH + "Special/SurfaceMaterial");
            LadderMaterial = Resources.Load<Material>(MATERIALS_BASE_PATH + "Special/LadderMaterial");


            Mat_Asphalt = Resources.Load<Material>(MATERIALS_BASE_PATH + "Asphalt");
            Mat_Brick = Resources.Load<Material>(MATERIALS_BASE_PATH + "Brick");
            Mat_Cliff = Resources.Load<Material>(MATERIALS_BASE_PATH + "Cliff");
            Mat_Cobblestone = Resources.Load<Material>(MATERIALS_BASE_PATH + "Cobblestone");
            Mat_ConcreteLight = Resources.Load<Material>(MATERIALS_BASE_PATH + "ConcreteLight");
            Mat_ConcreteDark = Resources.Load<Material>(MATERIALS_BASE_PATH + "ConcreteDark");
            Mat_Concrete2 = Resources.Load<Material>(MATERIALS_BASE_PATH + "Concrete2");
            Mat_DirtPath = Resources.Load<Material>(MATERIALS_BASE_PATH + "DirtPath");
            Mat_Glass = Resources.Load<Material>(MATERIALS_BASE_PATH + "Glass");
            Mat_Hedge = Resources.Load<Material>(MATERIALS_BASE_PATH + "Hedge");
            Mat_Plaster = Resources.Load<Material>(MATERIALS_BASE_PATH + "Plaster");
            Mat_RoofingTiles = Resources.Load<Material>(MATERIALS_BASE_PATH + "RoofingTiles");
            Mat_TilesBlue = Resources.Load<Material>(MATERIALS_BASE_PATH + "TilesBlue");
            Mat_TilesWhite = Resources.Load<Material>(MATERIALS_BASE_PATH + "TilesWhite");
            Mat_Water = Resources.Load<Material>(MATERIALS_BASE_PATH + "Water");
            Mat_Wood = Resources.Load<Material>(MATERIALS_BASE_PATH + "Wood");
            Mat_WoodParquet = Resources.Load<Material>(MATERIALS_BASE_PATH + "WoodParquet");
        }

        public Material BuildPreviewMaterial { get; private set; }

        public Material SurfaceMaterial { get; private set; }
        public Material LadderMaterial { get; private set; }

        public Material Mat_Asphalt { get; private set; }
        public Material Mat_Brick { get; private set; }
        public Material Mat_Cliff { get; private set; }
        public Material Mat_Cobblestone { get; private set; }
        public Material Mat_ConcreteLight { get; private set; }
        public Material Mat_ConcreteDark { get; private set; }
        public Material Mat_Concrete2 { get; private set; }
        public Material Mat_DirtPath { get; private set; }
        public Material Mat_Glass { get; private set; }
        public Material Mat_Hedge { get; private set; }
        public Material Mat_Plaster { get; private set; }
        public Material Mat_RoofingTiles { get; private set; }
        public Material Mat_TilesBlue { get; private set; }
        public Material Mat_TilesWhite { get; private set; }
        public Material Mat_Water { get; private set; }
        public Material Mat_Wood { get; private set; }
        public Material Mat_WoodParquet { get; private set; }

        [Header("Colors")]
        public Color GrassColor;
        public Color SandColor;
        public Color TarmacColor;
        public Color WaterColor;

        [Header("Textures")]
        public Texture2D GrassTexture;
        public Texture2D SandTexture;
        public Texture2D ConcreteTexture;
        public Texture2D WaterTexture;

        [Header("Prefabs")]
        public Projector SelectionIndicator;
    }
}
