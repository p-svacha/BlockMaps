using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFUi : MonoBehaviour
    {
        private CTFGame Game;

        [Header("Prefabs")]
        public UI_CharacterSelectionPanel CharacterSelectionPrefab;

        [Header("Elements")]
        public GameObject LoadingScreenOverlay;
        public GameObject CharacterSelectionContainer;
        public TextMeshProUGUI TileInfoText;

        private Dictionary<Character, UI_CharacterSelectionPanel> CharacterSelection = new();
        float deltaTime; // for fps

        public void Init(CTFGame game)
        {
            Game = game;
        }

        public void OnStartGame()
        {
            // Character selection
            HelperFunctions.DestroyAllChildredImmediately(CharacterSelectionContainer.gameObject);

            CharacterSelection.Clear();
            foreach (Character c in Game.Player.Characters)
            {
                UI_CharacterSelectionPanel panel = Instantiate(CharacterSelectionPrefab, CharacterSelectionContainer.transform);
                panel.Init(Game, c);
                CharacterSelection.Add(c, panel);
            }
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

        public void SelectCharacter(Character c)
        {
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(true); 
        }
        public void DeselectCharacter(Character c)
        {
            if (CharacterSelection.TryGetValue(c, out UI_CharacterSelectionPanel panel)) panel.SetSelected(false);
        }
    }
}
