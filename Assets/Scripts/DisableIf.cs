using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class DisableIf : PropertyAttribute
{

    public string attribute;
    public bool disabled;

    public DisableIf(string attribute, bool disabled)
    {
        this.attribute = attribute;
        this.disabled = disabled;
    }
}
