using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuOption
{
    public string Label { get; private set; }
    public System.Action Action { get; private set; }

    public ContextMenuOption(string label, Action action)
    {
        Label = label;
        Action = action;
    }
}
