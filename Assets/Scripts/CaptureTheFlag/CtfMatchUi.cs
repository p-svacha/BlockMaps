using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaptureTheFlag
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
        public Button EndTurnButton;

        public GameObject CharacterSelectionContainer;
        public UI_CharacterInfo CharacterInfo;
        public GameObject SpecialActionsContainer;

        public GameObject TurnIndicator;
        public TextMeshProUGUI TurnIndicatorText;

        public GameObject CharacterLabelsContainer;

        private Dictionary<CtfCharacter, UI_CharacterSelectionPanel> CharacterSelection = new();
        float deltaTime; // for fps

        public void Init(CtfMatch game)
        {
            Match = game;
            DevModeButton.onClick.AddListener(() => Match.ToggleDevMode());
            EndTurnButton.onClick.AddListener(() => Match.EndPlayerTurn());
        }

        public void OnGameInitialized()
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

            CharacterInfo.gameObject.SetActive(false);
            TurnIndicator.SetActive(false);
        }

        private void Update()
        {
            if (Match == null) return;

            string text = "";

            // Add coordinates
            if (Match != null && Match.DevMode && Match.World != null && Match.World.HoveredNode != null) text += "\n" + Match.World.HoveredNode;

            // Add FPS and tick
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

            if(Match.DevMode) text += "\nTick " + Match.CurrentTick;

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
            // Character Info
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(true);
            CharacterInfo.Init(c);

            // Actions
            SpecialActionsContainer.SetActive(true);
            HelperFunctions.DestroyAllChildredImmediately(SpecialActionsContainer);
            foreach(SpecialAction action in c.PossibleSpecialActions)
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
        }

        public void ShowRedNotificationText(string text, float hideAfter = 0f)
        {
            TurnIndicator.SetActive(true);
            TurnIndicatorText.text = text;
        }
        public void HideRedNotificationText()
        {
            TurnIndicator.SetActive(false);
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
