using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelPositioner : MonoBehaviour
{
    public List<PanelPosition> positions;
    RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void MoveTo(string positionName)
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (positions == null || positions.Count == 0) return;

        PanelPosition pos = positions.Find(x => x.name == positionName);
        if (pos == null) return;

        StopAllCoroutines();
        LeanTween.cancel(gameObject);

        // Só atualiza anchors se foram customizados
        if (pos.anchorMin != Vector2.zero || pos.anchorMax != Vector2.zero)
        {
            rect.anchorMin = pos.anchorMin;
            rect.anchorMax = pos.anchorMax;
        }

        // Move suavemente a anchoredPosition da UI
        Vector2 targetPos = new Vector2(pos.position.x, pos.position.y);
        LeanTween.value(gameObject, (Vector2 val) => {
            if (rect != null) rect.anchoredPosition = val;
        }, rect.anchoredPosition, targetPos, 0.25f).setEase(LeanTweenType.easeOutQuad);
    }
}
