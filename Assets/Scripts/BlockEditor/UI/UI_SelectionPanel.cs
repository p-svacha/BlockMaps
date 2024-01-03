using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class UI_SelectionPanel : MonoBehaviour
    {
        private const int ELEMENTS_PER_ROW = 6;

        [Header("Prefabs")]
        public UI_SelectionElement SelectionPrefab;

        [Header("Elements")]
        public GameObject Container;

        private UI_SelectionElement SelectedElement;
        private List<UI_SelectionElement> Elements = new List<UI_SelectionElement>();

        public void Clear()
        {
            HelperFunctions.DestroyAllChildredImmediately(Container);
        }

        public void AddElement(Sprite iconSprite, Color iconColor, string text, System.Action onSelectAction)
        {
            int rowIndex = Elements.Count / ELEMENTS_PER_ROW;
            if (rowIndex >= Container.transform.childCount) AddRow();

            UI_SelectionElement elem = Instantiate(SelectionPrefab, Container.transform.GetChild(rowIndex));
            elem.Init(iconSprite, iconColor, text);
            elem.Button.onClick.AddListener(() => SelectElement(elem));
            elem.Button.onClick.AddListener(onSelectAction.Invoke);

            Elements.Add(elem);
        }

        private void SelectElement(UI_SelectionElement elem)
        {
            if(SelectedElement != null) SelectedElement.SetSelected(false);
            SelectedElement = elem;
            SelectedElement.SetSelected(true);
        }
        private void AddRow()
        {
            GameObject newRow = new GameObject("row_" + (Container.transform.childCount + 1));
            newRow.transform.SetParent(Container.transform);
            HorizontalLayoutGroup hlg = newRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 8; // todo: make this dynamic according to container width & ELEMENTS_PER_ROW

            newRow.transform.localScale = Vector3.one;
        }
    }
}
