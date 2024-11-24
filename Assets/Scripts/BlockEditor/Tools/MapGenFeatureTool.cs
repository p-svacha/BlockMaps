using BlockmapFramework;
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
        private Sprite icon;
        public override Sprite Icon => icon;


        private List<MapGenFeatureDef> Features;

        [Header("Elements")]
        public TextMeshProUGUI DescriptionText;
        public TMP_Dropdown FeatureDropdown;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            icon = Resources.Load<Sprite>(IconBasePath + "feature");

            // Generator dropdown
            FeatureDropdown.onValueChanged.AddListener(FeatureDropdown_OnValueChanged);
            Features = DefDatabase<MapGenFeatureDef>.AllDefs.ToList();
            List<string> dropdownOptions = Features.Select(x => x.Label).ToList();
            FeatureDropdown.AddOptions(dropdownOptions);
            FeatureDropdown_OnValueChanged(FeatureDropdown.value);
        }
        private void FeatureDropdown_OnValueChanged(int value)
        {
            DescriptionText.text = Features[value].Description;
        }

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                bool canSpawn = World.HoveredNode.IsPassable();
                World.HoveredNode.ShowOverlay(ResourceManager.Singleton.TileSelector, canSpawn ? Color.white : Color.red);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            Features[FeatureDropdown.value].GenerateAction(World.HoveredNode);
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
