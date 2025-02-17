using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheThoriumChallenge
{
    public class UI_CreatureClassFlag : MonoBehaviour
    {
        [Header("Elements")]
        public Image Background;
        public TextMeshProUGUI Text;

        public void Init(CreatureClassDef classDef)
        {
            Background.color = classDef.Color;
            Text.text = classDef.LabelCap;
        }

    }
}
