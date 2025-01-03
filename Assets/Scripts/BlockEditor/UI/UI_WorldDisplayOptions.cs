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
        public Toggle VisionCutoffToggle;
        public TMP_InputField VisionCutoffAltitudeInput;

        public Button ResetExplorationButton;
        public Button ExploreEverythingButton;
        public Toggle GridToggle;
        public Toggle TextureToggle;
        public Toggle BlendToggle;
        public Toggle NavmeshToggle;

        public void Init(BlockEditor editor)
        {
            Editor = editor;

            VisionDropdown.onValueChanged.AddListener(VisionDropdown_OnValueChanged);
            ResetExplorationButton.onClick.AddListener(ResetExplorationButton_OnClick);
            ExploreEverythingButton.onClick.AddListener(ExploreEverythingButton_OnClick);

            GridToggle.onValueChanged.AddListener((b) => World.ShowGridOverlay(b));
            TextureToggle.onValueChanged.AddListener((b) => World.ShowTextures(b));
            BlendToggle.onValueChanged.AddListener((b) => World.ShowTileBlending(b));
            NavmeshToggle.onValueChanged.AddListener((b) => World.ShowNavmesh(b));
            VisionCutoffToggle.onValueChanged.AddListener((b) => World.ShowVisionCutoff(b));
        }
        public void OnNewWorld()
        {
            gameObject.SetActive(true);

            // Vision Dropdown
            VisionDropdown.ClearOptions();
            List<string> visionOptions = new List<string>() { "Everything" };
            foreach (Actor p in World.GetAllActors()) visionOptions.Add(p.Label);
            VisionDropdown.AddOptions(visionOptions);

            SetVisionCutoffAlitude(10);
            UpdateUiElements();
        }

        public void HandleKeyboardInputs()
        {
            // G - Toggle Grid
            if (Input.GetKeyDown(KeyCode.G))
            {
                World.ToggleGridOverlay();
                UpdateUiElements();
            }

            // N - Toggle Navmesh
            if (Input.GetKeyDown(KeyCode.N))
            {
                World.ToggleNavmesh();
                UpdateUiElements();
            }

            // T - Texture mode
            if (Input.GetKeyDown(KeyCode.T))
            {
                World.ToggleTextureMode();
                UpdateUiElements();
            }

            // B - Surface blending
            if (Input.GetKeyDown(KeyCode.B))
            {
                World.ToggleTileBlending();
                UpdateUiElements();
            }

            // V - Toggle vision cutoff
            if (Input.GetKeyDown(KeyCode.V))
            {
                World.ToggleVisionCutoff();
                UpdateUiElements();
            }

            // Alt + Scroll - Change vision cutoff altitude
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.mouseScrollDelta.y < 0) SetVisionCutoffAlitude(World.VisionCutoffAltitude - 1);
                if (Input.mouseScrollDelta.y > 0) SetVisionCutoffAlitude(World.VisionCutoffAltitude + 1);
            }
        }

        private void SetVisionCutoffAlitude(int alt)
        {
            World.SetVisionCutoffAltitude(alt);
            VisionCutoffAltitudeInput.text = World.VisionCutoffAltitude.ToString();
        }

        private void VisionDropdown_OnValueChanged(int value)
        {
            World.SetActiveVisionActor(GetSelectedPlayer());
        }
        private void ResetExplorationButton_OnClick()
        {
            Actor p = GetSelectedPlayer();
            if(p != null) World.ResetExploration(p);
        }
        private void ExploreEverythingButton_OnClick()
        {
            Actor p = GetSelectedPlayer();
            if (p != null) World.ExploreEverything(p);
        }

        private Actor GetSelectedPlayer()
        {
            if (VisionDropdown.value == 0) return null;
            else return World.GetActor(VisionDropdown.options[VisionDropdown.value].text);
        }

        public void UpdateUiElements()
        {
            gameObject.SetActive(World != null);
            if (World == null) return;

            if (World.ActiveVisionActor == null) VisionDropdown.value = 0;
            else VisionDropdown.value = VisionDropdown.options.Where(x => x.text == World.ActiveVisionActor.Label).Select(x => VisionDropdown.options.IndexOf(x)).First();

            GridToggle.isOn = World.IsShowingGrid;
            TextureToggle.isOn = World.IsShowingTextures;
            BlendToggle.isOn = World.IsShowingTileBlending;
            NavmeshToggle.isOn = World.IsShowingNavmesh;
            VisionCutoffToggle.isOn = World.IsVisionCutoffEnabled;
        }
    }
}
