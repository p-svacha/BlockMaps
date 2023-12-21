using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class EditorToolButton : MonoBehaviour
    {
        [Header("Elements")]
        public GameObject SelectedFrame;
        public EditorTool Tool;

        public void SetSelected(bool value)
        {
            SelectedFrame.gameObject.SetActive(value);
        }
    }
}
