﻿using System.Collections;
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
    private const int MouseOffset = 5;
    private const int ScreenEdgeOffset = 20;

    private int InitializeOffset; // We need to wait 2 frames before width and height can be read correctly

    public void Init(TooltipType type, string title, string text)
    {
        Title.gameObject.SetActive(type == TooltipType.TitleAndText);
        Separator.gameObject.SetActive(type == TooltipType.TitleAndText);

        Title.text = title;
        Text.text = text;

        Vector3 position = Input.mousePosition + new Vector3(MouseOffset, -MouseOffset, 0);
        transform.position = position;
    }

    /*
    private void Update()
    {
        if (InitializeOffset < 2) InitializeOffset++;
        else if (InitializeOffset == 2)
        {
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            Vector3 position = Input.mousePosition + new Vector3(MouseOffset, -MouseOffset, 0);
            Width = GetComponent<RectTransform>().rect.width;
            Height = GetComponent<RectTransform>().rect.height;
            if (position.x + Width > Screen.width) position.x = Screen.width - Width - ScreenEdgeOffset;
            if (position.y - Height < 0) position.y = Height + ScreenEdgeOffset;
            transform.position = position;
            InitializeOffset++;
        }
    }
    */

    public enum TooltipType
    {
        TitleAndText,
        TextOnly
    }
}
