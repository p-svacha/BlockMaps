using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaptureTheFlag
{
    public class CTFUi : MonoBehaviour
    {
        private CTFGame Game;

        [Header("Prefabs")]
        public UI_CharacterSelectionPanel CharacterSelectionPrefab;

        [Header("Elements")]
        public Button EndTurnButton;
        public GameObject LoadingScreenOverlay;

        public GameObject EndGameScreen;
        public TextMeshProUGUI EndGameText;
        public Button EndGameMenuButton;

        public GameObject CharacterSelectionContainer;
        public TextMeshProUGUI TileInfoText;
        public UI_CharacterInfo CharacterInfo;

        private Dictionary<Character, UI_CharacterSelectionPanel> CharacterSelection = new();
        float deltaTime; // for fps

        public void Init(CTFGame game)
        {
            Game = game;
            EndTurnButton.onClick.AddListener(() => Game.EndYourTurn());
        }

        public void OnStartGame()
        {
            // Character selection
            HelperFunctions.DestroyAllChildredImmediately(CharacterSelectionContainer.gameObject);

            CharacterSelection.Clear();
            foreach (Character c in Game.LocalPlayer.Characters)
            {
                UI_CharacterSelectionPanel panel = Instantiate(CharacterSelectionPrefab, CharacterSelectionContainer.transform);
                panel.Init(Game, c);
                CharacterSelection.Add(c, panel);
            }

            CharacterInfo.gameObject.SetActive(false);
        }

        private void Update()
        {
            string text = "";

            // Add FPS
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

            TileInfoText.text = text;
        }

        /// <summary>
        /// Updates the selection panel for a single character.
        /// </summary>
        public void UpdateSelectionPanel(Character c)
        {
            CharacterSelection[c].UpdateBars();
        }
        /// <summary>
        /// Updates the selection panel for all characters.
        /// </summary>
        public void UpdateSelectionPanels()
        {
            foreach (UI_CharacterSelectionPanel panel in CharacterSelection.Values) panel.UpdateBars();
        }

        public void SelectCharacter(Character c)
        {
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(true);
            CharacterInfo.Init(c);
        }
        public void DeselectCharacter(Character c)
        {
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(false);
            CharacterInfo.gameObject.SetActive(false);
        }

        public void ShowEndGameScreen(string text)
        {
            EndGameScreen.SetActive(true);
            EndGameText.text = text;
        }
    }
}
