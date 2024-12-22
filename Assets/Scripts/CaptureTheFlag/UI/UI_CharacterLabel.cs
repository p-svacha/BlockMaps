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

        [Header("Elements")]
        public TextMeshProUGUI NameText;

        public void Init(CtfCharacter c)
        {
            Character = c;
            NameText.text = c.LabelCap;
            NameText.color = c.Owner.Actor.Color;
            WorldOffset = new Vector3(0f, (c.Height / 2f) + 0.1f, 0f);
        }

        public void SetLabelText(string text)
        {
            NameText.text = text;
        }

        private void Update()
        {
            if (Character == null) return;

            // Check visibility
            NameText.gameObject.SetActive(Character.IsVisible);

            // Update position
            if (Width == 0) Width = GetComponent<RectTransform>().sizeDelta.x;
            Vector3 targetWorldPosition = Character.MeshObject.transform.position + WorldOffset;

            Vector3 screenPosition = Character.Game.World.Camera.Camera.WorldToScreenPoint(targetWorldPosition);
            screenPosition = screenPosition + new Vector3(-Width / 2, 0f, 0f);
            transform.position = screenPosition;
        }
    }
}
