using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterLabel : MonoBehaviour
    {
        private CtfCharacter Character;
        private Vector3 WorldOffset;
        public float Width;

        // Double click
        private float DoubleClickTimeThreshold = 0.5f;
        private float LastClickTime = 0f;

        [Header("Elements")]
        public Button Button;
        public TextMeshProUGUI NameText;
        public Image SelectedFrame;

        public void Init(CtfCharacter c)
        {
            Character = c;
            NameText.text = c.LabelCap;
            NameText.color = c.Actor.Color;
            SelectedFrame.color = c.Actor.Color;
            WorldOffset = new Vector3(0f, (c.Height / 2f) + 0.1f, 0f);

            Button.onClick.AddListener(OnClick);
            SetSelected(false);
        }

        private void OnClick()
        {
            if (Time.time - LastClickTime < DoubleClickTimeThreshold) // Double click detected
            {
                Character.World.CameraPanToFocusEntity(Character, duration: 0.5f, false);
            }
            else // Single click
            {
                Character.Match.SelectCharacter(Character);
            }

            LastClickTime = Time.time;
        }

        public void SetLabelText(string text)
        {
            NameText.text = text;
        }

        public void SetSelected(bool value)
        {
            SelectedFrame.gameObject.SetActive(value);
        }

        private void Update()
        {
            if (Character == null) return;

            bool showLabel = Character.IsVisible || Character.IsExploredBy(Character.Match.LocalPlayer.Actor);

            if (showLabel)
            {
                NameText.gameObject.SetActive(true);

                // Update position
                if (Width == 0) Width = GetComponent<RectTransform>().sizeDelta.x;
                Vector3 targetWorldPosition = Character.IsVisible ? Character.MeshObject.transform.position + WorldOffset : (Vector3)Character.LastKnownPosition[Character.Match.LocalPlayer.Actor] + WorldOffset;

                Vector3 screenPosition = Character.Match.World.Camera.Camera.WorldToScreenPoint(targetWorldPosition);
                screenPosition = screenPosition + new Vector3(-Width / 2, 0f, 0f);
                transform.position = screenPosition;

                // Transparent when not currently visible
                if (Character.IsVisible && NameText.color.a != 1f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 1f);
                else if (!Character.IsVisible && NameText.color.a != 0.5f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 0.5f);
            }

            else
            {
                NameText.gameObject.SetActive(false);
            }
        }
    }
}
