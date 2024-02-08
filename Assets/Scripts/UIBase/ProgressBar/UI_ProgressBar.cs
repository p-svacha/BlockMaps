using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UI_ProgressBar : MonoBehaviour
{
    private const float BLINK_SPEED = 1f; // blinks per second

    [Header("Elements")]
    public GameObject Container;
    public GameObject ProgressBar;
    public GameObject BlinkingBar;
    public TextMeshProUGUI ProgressText;

    private bool IsBlinking;
    private Color BlinkColor1;
    private Color BlinkColor2;

    private void Update()
    {
        if(IsBlinking)
        {
            // Calculate the blink value using Mathf.PingPong to create a looping effect
            float blinkValue = Mathf.PingPong(Time.time * BLINK_SPEED, 1.0f);

            Color c = HelperFunctions.SmoothLerpColor(BlinkColor1, BlinkColor2, blinkValue);
            BlinkingBar.GetComponent<Image>().color = c;
        }
    }

    public void SetValue(float value, float maxValue, bool showText = false, string valueFormat = "", bool reverse = false)
    {
        BlinkingBar.SetActive(false);
        IsBlinking = false;

        float ratio = value / maxValue;
        if (reverse) ratio = 1f - ratio;
        ProgressBar.GetComponent<RectTransform>().anchorMax = new Vector2(ratio, 1f);

        if (ProgressText != null)
        {
            if (showText) ProgressText.text = value.ToString(valueFormat) + " / " + maxValue.ToString(valueFormat);
            else ProgressText.text = "";
        }
    }

    /// <summary>
    /// Shows a pending value by making the difference blink.
    /// </summary>
    public void SetPendingValue(float oldValue, float newValue, float maxValue, string valueFormat, Color blinkColor1, Color blinkColor2)
    {
        BlinkColor1 = blinkColor1;
        BlinkColor2 = blinkColor2;
        IsBlinking = oldValue != newValue;
        BlinkingBar.SetActive(IsBlinking);

        float oldRatio = oldValue / maxValue;
        float newRatio = newValue / maxValue;
        
        ProgressBar.GetComponent<RectTransform>().anchorMax = new Vector2(oldRatio, 1f);

        BlinkingBar.GetComponent<RectTransform>().anchorMin = new Vector2(Mathf.Min(oldRatio, newRatio), 0f);
        BlinkingBar.GetComponent<RectTransform>().anchorMax = new Vector2(Mathf.Max(oldRatio, newRatio), 1f);

        ProgressText.text = newValue.ToString(valueFormat) + " / " + maxValue.ToString(valueFormat);
    }

    public void SetBarColor(Color color)
    {
        ProgressBar.GetComponent<Image>().color = color;
    }
}
