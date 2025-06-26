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

        [Header("Camera Info")]
        public TMP_InputField CameraPositionInput;
        public TMP_InputField CameraZoomInput;
        public TMP_InputField CameraAngleInput;
        public TMP_InputField CameraDirectionInput;

        [Header("Display Settings")]
        public TMP_Dropdown VisionDropdown;
        public TMP_Dropdown VisionCutoffDropdown;
        public TMP_InputField VisionCutoffAltitudeInput;
        public TMP_InputField VisionCutoffPerspectiveHeightInput;
        public TMP_InputField VisionCutoffTargetInput;

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
            VisionCutoffDropdown.onValueChanged.AddListener((v) => World.SetVisionCutoffMode((VisionCutoffMode)v));
            VisionCutoffAltitudeInput.onValueChanged.AddListener(VisionCutoffAltitudeInput_OnValueChanged);
            VisionCutoffPerspectiveHeightInput.onValueChanged.AddListener(VisionCutoffHeightInput_OnValueChanged);

            // Vision cutoff dropdown
            List<string> visionCutoffOptions = new List<string>();
            foreach(VisionCutoffMode mode in System.Enum.GetValues(typeof(VisionCutoffMode))) visionCutoffOptions.Add(mode.ToString());
            VisionCutoffDropdown.AddOptions(visionCutoffOptions);
        }
        public void OnNewWorld()
        {
            gameObject.SetActive(true);

            // Vision Dropdown
            VisionDropdown.ClearOptions();
            List<string> visionOptions = new List<string>() { "Everything" };
            foreach (Actor p in World.GetAllActors()) visionOptions.Add(p.Label);
            VisionDropdown.AddOptions(visionOptions);

            RefreshSettings();
        }

        private void Update()
        {
            UpdateCameraInfo();
        }
        public void HandleKeyboardInputs()
        {
            // G - Toggle Grid
            if (Input.GetKeyDown(KeyCode.G))
            {
                World.ToggleGridOverlay();
                RefreshSettings();
            }

            // N - Toggle Navmesh
            if (Input.GetKeyDown(KeyCode.N))
            {
                World.ToggleNavmesh();
                RefreshSettings();
            }

            // T - Texture mode
            if (Input.GetKeyDown(KeyCode.T))
            {
                World.ToggleTextureMode();
                RefreshSettings();
            }

            // B - Surface blending
            if (Input.GetKeyDown(KeyCode.B))
            {
                World.ToggleTileBlending();
                RefreshSettings();
            }

            // V - Enable perspective vision cutoff
            if(Input.GetKeyDown(KeyCode.V))
            {
                if(World.HoveredEntity != null)
                {
                    World.EnablePerspectiveVisionCutoff(World.HoveredEntity);
                    RefreshSettings();
                }
            }

            // Alt + Scroll - Change vision cutoff altitude
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.mouseScrollDelta.y < 0)
                {
                    SetVisionCutoffAlitude(World.DisplaySettings.VisionCutoffAltitude - 1);
                }
                if (Input.mouseScrollDelta.y > 0)
                {
                    SetVisionCutoffAlitude(World.DisplaySettings.VisionCutoffAltitude + 1);
                }
            }
        }

        #region Camera Info

        private void UpdateCameraInfo()
        {
            CameraPositionInput.text = BlockmapCamera.Instance.CurrentPosition.ToString();
            CameraZoomInput.text = BlockmapCamera.Instance.CurrentZoom.ToString("0.#");
            CameraAngleInput.text = BlockmapCamera.Instance.CurrentAngle.ToString("0.#");
            CameraDirectionInput.text = BlockmapCamera.Instance.CurrentFacingDirection.ToString();
        }

        #endregion


        #region Display Settings

        private void VisionCutoffAltitudeInput_OnValueChanged(string value)
        {
            if(int.TryParse(value, out int v)) World.SetVisionCutoffAltitude(v);
        }
        private void VisionCutoffHeightInput_OnValueChanged(string value)
        {
            if (int.TryParse(value, out int v)) World.SetVisionCutoffPerspectiveHeight(v);
        }
        private void SetVisionCutoffAlitude(int alt)
        {
            World.SetVisionCutoffAltitude(alt);
            VisionCutoffAltitudeInput.text = World.DisplaySettings.VisionCutoffAltitude.ToString();
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

        public void RefreshSettings()
        {
            gameObject.SetActive(World != null);
            if (World == null) return;

            if (World.ActiveVisionActor == null) VisionDropdown.value = 0;
            else VisionDropdown.value = VisionDropdown.options.Where(x => x.text == World.ActiveVisionActor.Label).Select(x => VisionDropdown.options.IndexOf(x)).First();

            GridToggle.isOn = World.DisplaySettings.IsShowingGrid;
            TextureToggle.isOn = World.DisplaySettings.IsShowingTextures;
            BlendToggle.isOn = World.DisplaySettings.IsShowingTileBlending;
            NavmeshToggle.isOn = World.DisplaySettings.IsShowingNavmesh;
            VisionCutoffDropdown.value = (int)World.DisplaySettings.VisionCutoffMode;
            VisionCutoffAltitudeInput.text = World.DisplaySettings.VisionCutoffAltitude.ToString();
            VisionCutoffPerspectiveHeightInput.text = World.DisplaySettings.VisionCutoffPerpectiveHeight.ToString();
            VisionCutoffTargetInput.text = World.DisplaySettings.PerspectiveVisionCutoffTarget?.LabelCap;
        }

        #endregion
    }
}
