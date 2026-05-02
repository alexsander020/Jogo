using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [HideInInspector]

    public Stats stats;

    void Awake()
    {
        stats = GetComponent<Stats>();
    }

}
