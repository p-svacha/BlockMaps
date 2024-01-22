using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;
using System.Linq;

namespace WorldEditor
{
    public class UI_WorldDisplayOptions : MonoBehaviour
    {
        private BlockEditor Editor;
        private World World => Editor.World;

        [Header("Elements")]
        public TMP_Dropdown VisionDropdown;
        public Button ResetExplorationButton;
        public Toggle GridToggle;
        public Toggle TextureToggle;
        public Toggle BlendToggle;
        public Toggle NavmeshToggle;

        public void Init(BlockEditor editor)
        {
            Editor = editor;

            List<string> visionOptions = new List<string>() { "Everything" };
            foreach (Player p in World.Players.Values) visionOptions.Add(p.Name);
            VisionDropdown.AddOptions(visionOptions);

            VisionDropdown.onValueChanged.AddListener(VisionDropdown_OnValueChanged);
            GridToggle.onValueChanged.AddListener((b) => World.ShowGridOverlay(b));
            TextureToggle.onValueChanged.AddListener((b) => World.ShowTextures(b));
            BlendToggle.onValueChanged.AddListener((b) => World.ShowTileBlending(b));
            NavmeshToggle.onValueChanged.AddListener((b) => World.ShowNavmesh(b));
            ResetExplorationButton.onClick.AddListener(ResetExplorationButton_OnClick);

            UpdateValues();
        }

        private void VisionDropdown_OnValueChanged(int value)
        {
            World.SetActiveVisionPlayer(GetSelectedPlayer());
        }
        private void ResetExplorationButton_OnClick()
        {
            Player p = GetSelectedPlayer();
            if(p != null) World.ResetExploration(p);
        }

        private Player GetSelectedPlayer()
        {
            if (VisionDropdown.value == 0) return null;
            else return World.Players.Values.First(x => x.Name == VisionDropdown.options[VisionDropdown.value].text);
        }

        public void UpdateValues()
        {
            if (World.ActiveVisionPlayer == null) VisionDropdown.value = 0;
            else VisionDropdown.value = VisionDropdown.options.Where(x => x.text == World.ActiveVisionPlayer.Name).Select(x => VisionDropdown.options.IndexOf(x)).First();

            GridToggle.isOn = World.IsShowingGrid;
            TextureToggle.isOn = World.IsShowingTextures;
            BlendToggle.isOn = World.IsShowingTileBlending;
            NavmeshToggle.isOn = World.IsShowingNavmesh;
        }
    }
}
