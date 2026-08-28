using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    const float MoveSpeed = 0.4f;
    float jumpHeight = 0.5f;
    public bool teste;

    public List<Vector3Int> path;
    SpriteRenderer SR;
    Transform jumper;
    TileLogic tileAtual;
    Unit unit;

    void Awake()
    {
        jumper = transform.Find("Jumper");
        if (jumper == null) jumper = transform.Find("jumpe");
        if (jumper == null) jumper = transform;

        SR = GetComponentInChildren<SpriteRenderer>();
        unit = GetComponent<Unit>();
    }

    void Update()
    {
        if (teste)
        {
            teste = false;
            StopAllCoroutines();
            StartCoroutine(Traverse(path));
        }
    }

    public IEnumerator Traverse(List<Vector3Int> pathList)
    {
        if (pathList == null || pathList.Count == 0) yield break;

        tileAtual = Board.GetTile(pathList[0]);
        if (tileAtual != null)
        {
            transform.position = tileAtual.worldPos;
            if (unit != null) unit.PlaceAtTile(tileAtual);
        }

        for (int i = 1; i < pathList.Count; i++)
        {
            TileLogic to = Board.GetTile(pathList[i]);
            if (to == null) continue;

            // Atualiza orientação para a direção do movimento
            if (unit != null)
            {
                FacingDirection moveDir = DirectionUtils.VectorToDirection(to.pos - tileAtual.pos);
                unit.SetFacing(moveDir);
            }

            if (tileAtual != null)
            {
                tileAtual.content = null;
            }

            if (tileAtual != null && to.floor != null && tileAtual.floor != null && tileAtual.floor != to.floor)
            {
                yield return Jump(to);
            }
            else
            {
                yield return Walk(to);
            }
        }

        if (unit != null && tileAtual != null)
        {
            unit.PlaceAtTile(tileAtual);
            unit.hasMoved = true;
        }
    }

    IEnumerator Walk(TileLogic to)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = to.worldPos;
        float elapsed = 0f;

        tileAtual = to;

        while (elapsed < MoveSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / MoveSpeed);
            transform.position = Vector3.Lerp(startPos, endPos, t);

            if (t >= 0.5f && SR != null)
            {
                SR.sortingOrder = to.contentOrder;
            }

            yield return null;
        }

        transform.position = endPos;
        if (SR != null)
        {
            SR.sortingOrder = to.contentOrder;
        }
        to.content = this.gameObject;
    }

    IEnumerator Jump(TileLogic to)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = to.worldPos;
        float elapsed = 0f;

        tileAtual = to;

        Vector3 jumperStartLocalPos = jumper != null && jumper != transform ? jumper.localPosition : Vector3.zero;

        while (elapsed < MoveSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / MoveSpeed);
            transform.position = Vector3.Lerp(startPos, endPos, t);

            if (jumper != null && jumper != transform)
            {
                float arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                jumper.localPosition = jumperStartLocalPos + new Vector3(0, arc, 0);
            }

            if (t >= 0.5f && SR != null)
            {
                SR.sortingOrder = to.contentOrder;
            }

            yield return null;
        }

        transform.position = endPos;
        if (jumper != null && jumper != transform)
        {
            jumper.localPosition = jumperStartLocalPos;
        }
        if (SR != null)
        {
            SR.sortingOrder = to.contentOrder;
        }
        to.content = this.gameObject;
    }
}