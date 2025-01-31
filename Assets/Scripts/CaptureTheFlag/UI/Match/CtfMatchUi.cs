using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaptureTheFlag.UI
{
    public class CtfMatchUi : MonoBehaviour
    {
        private CtfMatch Match;

        [Header("Prefabs")]
        public UI_CharacterSelectionPanel CharacterSelectionPrefab;
        public UI_CharacterAction CharacterActionButtonPrefab;
        public UI_CharacterLabel CharacterLabelPrefab;

        [Header("Elements")]
        public Button DevModeButton;

        public TextMeshProUGUI TileInfoText;
        public UI_ToggleButton ToggleGridButton;
        public Button EndTurnButton;

        public GameObject CharacterSelectionContainer;
        public UI_CharacterInfo CharacterInfo;
        public GameObject SpecialActionsContainer;

        public GameObject CenterNotificationContainer;
        public TextMeshProUGUI CenterNotificationText;

        public GameObject CharacterLabelsContainer;
        public UI_GameOverPanel GameOverPanel;

        private Dictionary<CtfCharacter, UI_CharacterSelectionPanel> CharacterSelection = new();
        float deltaTime; // for fps

        public void Init(CtfMatch match)
        {
            Match = match;

            CharacterInfo.Init(Match);

            DevModeButton.onClick.AddListener(() => Match.ToggleDevMode());
            ToggleGridButton.Button.onClick.AddListener(() => { Match.World.ToggleGridOverlay(); ToggleGridButton.SetToggle(Match.World.DisplaySettings.IsShowingGrid); });
            EndTurnButton.onClick.AddListener(() => Match.EndPlayerTurn());

            GameOverPanel.Init(match);
        }

        public void OnMatchReady()
        {
            // Character selection
            HelperFunctions.DestroyAllChildredImmediately(CharacterSelectionContainer.gameObject);

            CharacterSelection.Clear();
            foreach (CtfCharacter c in Match.LocalPlayer.Characters)
            {
                UI_CharacterSelectionPanel panel = Instantiate(CharacterSelectionPrefab, CharacterSelectionContainer.transform);
                panel.Init(Match, c);
                CharacterSelection.Add(c, panel);
            }

            HelperFunctions.DestroyAllChildredImmediately(SpecialActionsContainer);
            CharacterInfo.gameObject.SetActive(false);
            CenterNotificationContainer.SetActive(false);
        }

        private void Update()
        {
            if (Match == null) return;

            CharacterInfo.UpdateCharacterInfo();
            UpdateHoverInfoText();
        }

        private void UpdateHoverInfoText()
        {
            string text = "";

            if(Match.DevMode)
            {
                // Add coordinates
                if (Match.World != null && Match.World.HoveredNode != null) text += "\n" + Match.World.HoveredNode;

                // Add FPS and tick
                deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
                float fps = 1.0f / deltaTime;
                text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

                text += "\nTick " + Match.CurrentTick;
            }
            else
            {
                if (Match.World != null && Match.World.HoveredNode != null)
                {
                    text = $"{Match.World.HoveredNode.SurfaceDef.LabelCap} ({Match.World.HoveredNode.SurfaceDef.MovementSpeedModifier.ToString("P0")} speed)";
                }
            }

            TileInfoText.text = text;
        }

        /// <summary>
        /// Updates the selection panel for a single character.
        /// </summary>
        public void UpdateSelectionPanel(CtfCharacter c)
        {
            CharacterSelection[c].Refresh();
        }
        /// <summary>
        /// Updates the selection panel for all characters.
        /// </summary>
        public void UpdateSelectionPanels()
        {
            foreach (UI_CharacterSelectionPanel panel in CharacterSelection.Values) panel.Refresh();
        }

        public void SelectCharacter(CtfCharacter c)
        {
            // Character Selection
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(true);

            // Character Label
            c.UI_Label.SetSelected(true);

            // Actions
            SpecialActionsContainer.SetActive(true);
            HelperFunctions.DestroyAllChildredImmediately(SpecialActionsContainer);
            foreach(SpecialCharacterAction action in c.PossibleSpecialActions)
            {
                UI_CharacterAction actionBtn = Instantiate(CharacterActionButtonPrefab, SpecialActionsContainer.transform);
                actionBtn.Init(action);
            }
        }
        public void DeselectCharacter(CtfCharacter c)
        {
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(false);
            CharacterInfo.gameObject.SetActive(false);
            SpecialActionsContainer.SetActive(false);
            c.UI_Label.SetSelected(false);
        }

        public void ShowCenterNotification(string text, Color color)
        {
            CenterNotificationContainer.SetActive(true);
            CenterNotificationText.text = text;
            CenterNotificationText.color = color;
        }
        public void HideCenterNotification()
        {
            CenterNotificationContainer.SetActive(false);
        }

        public void ShowEndGameScreen(string text)
        {
            GameOverPanel.gameObject.SetActive(true);
            GameOverPanel.Text.text = text;
        }

        public void OnSetDevMode(bool active)
        {
            if(active)
            {
                DevModeButton.GetComponent<Image>().color = Color.white;
                DevModeButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
            }
            else
            {
                DevModeButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
                DevModeButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.2f);
            }
        }
    }
}
