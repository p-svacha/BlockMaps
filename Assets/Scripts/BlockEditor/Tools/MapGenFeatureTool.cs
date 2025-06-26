using BlockmapFramework;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace WorldEditor
{
    public class MapGenFeatureTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.MapGenFeature;
        public override string Name => "Place Feature";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "MapGenFeature");

        private List<MapGenFeatureDef> Features;

        private Parcel Parcel;
        public GameObject ParcelPreview;

        private int ParcelSizeX => int.Parse(ParcelInputX.text);
        private int ParcelSizeY => int.Parse(ParcelInputY.text);

        [Header("Elements")]
        public TextMeshProUGUI DescriptionText;
        public TMP_Dropdown FeatureDropdown;
        public TMP_InputField ParcelInputX;
        public TMP_InputField ParcelInputY;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            // Generator dropdown
            FeatureDropdown.onValueChanged.AddListener(FeatureDropdown_OnValueChanged);
            Features = DefDatabase<MapGenFeatureDef>.AllDefs.ToList();
            List<string> dropdownOptions = Features.Select(x => x.LabelCap).ToList();
            FeatureDropdown.AddOptions(dropdownOptions);
            FeatureDropdown_OnValueChanged(FeatureDropdown.value);
        }
        private void FeatureDropdown_OnValueChanged(int value)
        {
            DescriptionText.text = Features[value].Description;
        }

        public override void UpdateTool()
        {
            UpdateParcelSizeWithInput();

            if(World.HoveredNode != null) World.HoveredNode.ShowOverlay(ResourceManager.FullTileSelector, Color.white);
        }

        private void UpdateParcelSizeWithInput()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetParcelSizeX(ParcelSizeX - 1);
                if (Input.mouseScrollDelta.y > 0) SetParcelSizeX(ParcelSizeX + 1);
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.mouseScrollDelta.y < 0) SetParcelSizeY(ParcelSizeY - 1);
                if (Input.mouseScrollDelta.y > 0) SetParcelSizeY(ParcelSizeY + 1);
            }
        }

        private void SetParcelSizeX(int value)
        {
            value = Mathf.Clamp(value, 1, 20);
            ParcelInputX.text = value.ToString();
            UpdateParcelAndPreview();
        }
        private void SetParcelSizeY(int value)
        {
            value = Mathf.Clamp(value, 1, 20);
            ParcelInputY.text = value.ToString();
            UpdateParcelAndPreview();
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (Parcel == null) return;

            Features[FeatureDropdown.value].GenerateAction(World, Parcel, World.HoveredNode, true);
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
            UpdateParcelAndPreview();
        }

        public override void OnSelect()
        {
            ParcelPreview = new GameObject("ParcelPreview");
        }

        public override void OnDeselect()
        {
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
            Destroy(ParcelPreview);
        }


        private void UpdateParcelAndPreview()
        {
            Parcel = null;

            if (World.HoveredNode == null) return;
            if (ParcelInputX.text == "") return;
            if (ParcelInputY.text == "") return;


            int worldCoordsX = World.HoveredNode.WorldCoordinates.x - ParcelSizeX / 2;
            int worldCoordsY = World.HoveredNode.WorldCoordinates.y - ParcelSizeY / 2;
            Parcel = new Parcel(new Vector2Int(worldCoordsX, worldCoordsY), new Vector2Int(ParcelSizeX, ParcelSizeY));
            float worldAltitude = World.HoveredNode.BaseWorldAltitude;
            float previewHeight = 2f;

            MeshBuilder meshBuilder = new MeshBuilder(ParcelPreview);
            int submesh = meshBuilder.GetSubmesh(MaterialManager.BuildPreviewMaterial);

            CreateParcelPreviewBorder(meshBuilder, submesh, Parcel.CornerSW, Parcel.CornerSE, worldAltitude, previewHeight);
            CreateParcelPreviewBorder(meshBuilder, submesh, Parcel.CornerSE, Parcel.CornerNE, worldAltitude, previewHeight);
            CreateParcelPreviewBorder(meshBuilder, submesh, Parcel.CornerNE, Parcel.CornerNW, worldAltitude, previewHeight);
            CreateParcelPreviewBorder(meshBuilder, submesh, Parcel.CornerNW, Parcel.CornerSW, worldAltitude, previewHeight);

            meshBuilder.ApplyMesh(addCollider: false, castShadows: false);
            ParcelPreview.GetComponent<MeshRenderer>().material.color = Color.white;
        }
        private void CreateParcelPreviewBorder(MeshBuilder meshBuilder, int submesh, Vector2 start, Vector2 end, float worldAltitude, float height)
        {
            Vector3 v1 = new Vector3(start.x, worldAltitude, start.y);
            Vector3 v2 = new Vector3(end.x, worldAltitude, end.y);
            Vector3 v3 = new Vector3(end.x, worldAltitude + height, end.y);
            Vector3 v4 = new Vector3(start.x, worldAltitude + height, start.y);
            meshBuilder.BuildPlane(submesh, v1, v2, v3,v4, Vector2.zero, Vector2.zero);
            meshBuilder.BuildPlane(submesh, v4, v3, v2, v1, Vector2.zero, Vector2.zero);
        }
    }
}
