using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockmapFramework
{
    public class UI_EntityInfoWindow : MonoBehaviour
    {
        // Singleton
        public static UI_EntityInfoWindow Instance;
        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
            CloseButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI DescriptionText;
        public Button CloseButton;

        public GameObject StatListContainer;
        public TextMeshProUGUI StatDetailsText;

        [Header("Prefabs")]
        public UI_EntityInfoWindow_StatRow StatRowPrefab;

        public void Show(Entity e)
        {
            gameObject.SetActive(true);
            TitleText.text = e.LabelCap;
            DescriptionText.text = e.Description;

            HelperFunctions.DestroyAllChildredImmediately(StatListContainer);
            foreach(Stat stat in e.GetAllStats())
            {
                UI_EntityInfoWindow_StatRow statRow = Instantiate(StatRowPrefab, StatListContainer.transform);
                statRow.Init(this, stat);
            }

            StatDetailsText.text = "";
        }

        public void ShowStatDetails(Stat stat)
        {
            StatDetailsText.text = stat.GetBreakdownString();
        }
    }
}
