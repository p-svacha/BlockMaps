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
        public GameObject CharacterSelectionContainer;
        public TextMeshProUGUI TileInfoText;

        float deltaTime; // for fps

        public void Init(CTFGame game)
        {
            Game = game;
        }

        public void OnStartGame()
        {
            HelperFunctions.DestroyAllChildredImmediately(CharacterSelectionContainer.gameObject);

            foreach(Character c in Game.Player.Characters)
            {
                UI_CharacterSelectionPanel panel = Instantiate(CharacterSelectionPrefab, CharacterSelectionContainer.transform);
                panel.Init(Game, c);
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
    }
}
