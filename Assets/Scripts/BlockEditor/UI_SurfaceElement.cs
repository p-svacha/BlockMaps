using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace WorldEditor
{
    public class UI_SurfaceElement : MonoBehaviour
    {
        [Header("Elements")]
        public Image Icon;
        public Button Button;
        public GameObject SelectionFrame;
        public TextMeshProUGUI Text;

        public void Init(SurfacePaintTool tool, SurfaceId surfaceId)
        {
            Surface surface = SurfaceManager.Instance.GetSurface(surfaceId);
            Icon.color = surface.Color;
            SelectionFrame.SetActive(false);
            Text.text = surface.Name;
            Button.onClick.AddListener(() => tool.SelectSurface(surface.Id));
        }

        public void SetSelected(bool value)
        {
            SelectionFrame.SetActive(value);
        }
    }
}
