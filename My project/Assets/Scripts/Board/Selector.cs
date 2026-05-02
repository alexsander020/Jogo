using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Selector : MonoBehaviour
{
   public static Selector Instance;
   public Vector3Int position { get { return tile.pos; } }
   public TileLogic tile;
   public SpriteRenderer spriteRenderer;

    void Awake()
   {
        Instance = this;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

}
