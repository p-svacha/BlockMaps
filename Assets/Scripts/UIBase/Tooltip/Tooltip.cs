using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Tooltip : MonoBehaviour
{
    // Singleton
    public static Tooltip Instance;
    private void Awake()
    {
        Instance = GameObject.Find("Tooltip").GetComponent<Tooltip>();
        gameObject.SetActive(false);
    }

    [Header("Elements")]
    public TextMeshProUGUI Title;
    public TextMeshProUGUI Separator;
    public TextMeshProUGUI Text;

    public float Width;
    public float Height;
    private const int MOUSE_OFFSET = 5; // px
    private const int SCREEN_EDGE_OFFSET = 5; // px

    public void Init(TooltipType type, string title, string text)
    {
        Title.gameObject.SetActive(type == TooltipType.TitleAndText);
        Separator.gameObject.SetActive(type == TooltipType.TitleAndText);

        Title.text = title;
        Text.text = text;

        Vector3 position = Input.mousePosition + new Vector3(MOUSE_OFFSET, MOUSE_OFFSET, 0);
        Width = GetComponent<RectTransform>().rect.width;
        Height = GetComponent<RectTransform>().rect.height;
        if (position.x + Width > Screen.width - SCREEN_EDGE_OFFSET) position.x = Screen.width - Width - SCREEN_EDGE_OFFSET;
        if (position.y - Height < SCREEN_EDGE_OFFSET) position.y = Height + SCREEN_EDGE_OFFSET;
        if (position.y + Height > Screen.height - SCREEN_EDGE_OFFSET) position.y = Screen.height - Height - SCREEN_EDGE_OFFSET;
        transform.position = position;
    }


    public enum TooltipType
    {
        TitleAndText,
        TextOnly
    }
}

