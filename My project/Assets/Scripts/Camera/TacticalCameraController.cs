using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TacticalCameraController : MonoBehaviour
{
    public static TacticalCameraController Instance;

    [Header("Configurações de Foco e Enquadramento")]
    public Transform target;
    public Vector3 offset = new Vector3(0.5f, 0.5f, -10f); // Leve ajuste para centralizar o grid isométrico
    public float smoothSpeed = 4.5f;

    [Header("Configurações de Zoom")]
    public float targetZoom = 3.6f;     // Zoom aproximado e cinematográfico (Estilo Digimon Survive)
    public float minZoom = 2.5f;        // Zoom máximo aproximado
    public float maxZoom = 6.0f;        // Zoom distante para visão geral
    public float zoomSpeed = 5.0f;

    private Camera cam;
    private bool isManualPan = false;
    private Vector3 manualPanOrigin;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = targetZoom;
        }
    }

    void Start()
    {
        // Centraliza a câmera no centro do tabuleiro inicialmente
        CenterOnBoard();
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // 1. Zoom suave
        HandleZoom();

        // 2. Movimento suave da câmera para o alvo ou centro
        if (!isManualPan && target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.z = offset.z; // Mantém a profundidade da câmera constante

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }

    void HandleZoom()
    {
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            Vector2 scrollVal = mouse.scroll.ReadValue();
            if (Mathf.Abs(scrollVal.y) > 0.01f)
            {
                scroll = Mathf.Sign(scrollVal.y) * 0.35f;
            }
        }

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed)
            {
                scroll += 0.03f;
            }
            if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed)
            {
                scroll -= 0.03f;
            }
        }
#endif

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetZoom -= scroll * 2.0f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    // Foca suavemente em uma unidade ou objeto específico
    public void FocusOn(Transform newTarget)
    {
        target = newTarget;
        isManualPan = false;
    }

    // Foca em um tile específico do tabuleiro
    public void FocusOnTile(TileLogic tile)
    {
        if (tile == null) return;
        FocusOnPosition(tile.worldPos);
    }

    // Foca em uma posição de mundo arbitrária
    public void FocusOnPosition(Vector3 worldPos)
    {
        isManualPan = false;
        target = null;
        StopAllCoroutines();
        StartCoroutine(SmoothMoveTo(worldPos + offset));
    }

    // Centraliza a câmera no ponto médio de todos os tiles do tabuleiro
    public void CenterOnBoard()
    {
        if (Board.instance == null || Board.instance.tiles == null || Board.instance.tiles.Count == 0)
        {
            return;
        }

        Vector3 sumPos = Vector3.zero;
        int count = 0;

        foreach (var tile in Board.instance.tiles.Values)
        {
            sumPos += tile.worldPos;
            count++;
        }

        if (count > 0)
        {
            Vector3 center = sumPos / count;
            Vector3 targetPos = center + offset;
            targetPos.z = offset.z;

            transform.position = targetPos;
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = targetZoom;
            }
        }
    }

    IEnumerator SmoothMoveTo(Vector3 targetPos)
    {
        targetPos.z = offset.z;
        float elapsed = 0f;
        float duration = 0.35f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }
}
