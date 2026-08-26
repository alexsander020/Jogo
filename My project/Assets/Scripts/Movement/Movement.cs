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
                yield return StartCoroutine(Jump(to));
            }
            else
            {
                yield return StartCoroutine(Walk(to));
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
        int id = LeanTween.move(gameObject, to.worldPos, MoveSpeed).id;
        tileAtual = to;

        yield return new WaitForSeconds(MoveSpeed * 0.5f);
        if (SR != null)
        {
            SR.sortingOrder = to.contentOrder;
        }

        while (LeanTween.descr(id) != null)
        {
            yield return null;
        }
        to.content = this.gameObject;
    }

    IEnumerator Jump(TileLogic to)
    {
        int id1 = LeanTween.move(gameObject, to.worldPos, MoveSpeed).id;
        if (jumper != null)
        {
            LeanTween.moveLocalY(jumper.gameObject, jumpHeight, MoveSpeed * 0.5f)
                .setLoopPingPong(1).setEase(LeanTweenType.easeInOutQuad);
        }

        float timerOrderUpdate = MoveSpeed;
        if (tileAtual.floor != null && to.floor != null && tileAtual.floor.tilemap.tileAnchor.y > to.floor.tilemap.tileAnchor.y)
        {
            timerOrderUpdate *= 0.85f;
        }
        else
        {
            timerOrderUpdate *= 0.2f;
        }

        yield return new WaitForSeconds(timerOrderUpdate);

        tileAtual = to;
        if (SR != null)
        {
            SR.sortingOrder = to.contentOrder;
        }

        while (LeanTween.descr(id1) != null)
        {
            yield return null;
        }
        to.content = this.gameObject;
    }
}