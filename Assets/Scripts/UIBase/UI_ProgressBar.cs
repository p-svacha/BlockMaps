using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UI_ProgressBar : MonoBehaviour
{
    [Header("Elements")]
    public GameObject Container;
    public GameObject ProgressBar;
    public TextMeshProUGUI ProgressText;

    public void UpdateValues(float value, float maxValue, string text = "", bool reverse = false)
    {
        float ratio = value / maxValue;
        if (reverse) ratio = 1f - ratio;
        ProgressBar.GetComponent<RectTransform>().anchorMax = new Vector2(ratio, 1f);
        if (ProgressText != null) ProgressText.text = text;
    }

    public void SetBarColor(Color color)
    {
        ProgressBar.GetComponent<Image>().color = color;
    }
}
