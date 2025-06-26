using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class WorldModifierTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.WorldModifier;
        public override string Name => "Apply World Modifiers";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "WorldModifier");

        private List<WorldModifierDef> Modifiers;

        [Header("Elements")]
        public TextMeshProUGUI DescriptionText;
        public TMP_Dropdown ModifierDropdown;
        public Button ApplyButton;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            // Modifier dropdown
            ModifierDropdown.onValueChanged.AddListener(ModifierDropdown_OnValueChanged);
            Modifiers = DefDatabase<WorldModifierDef>.AllDefs.ToList();
            List<string> dropdownOptions = Modifiers.Select(x => x.LabelCap).ToList();
            ModifierDropdown.AddOptions(dropdownOptions);
            ModifierDropdown_OnValueChanged(ModifierDropdown.value);

            // Apply button
            ApplyButton.onClick.AddListener(ApplyButton_OnClick);
        }

        private void ModifierDropdown_OnValueChanged(int value)
        {
            DescriptionText.text = Modifiers[value].Description;
        }

        private void ApplyButton_OnClick()
        {
            WorldModifierDef selectedModifier = Modifiers[ModifierDropdown.value];
            selectedModifier.ModifierAction(World);
            World.UpdateFullWorld();
        }
    }
}
