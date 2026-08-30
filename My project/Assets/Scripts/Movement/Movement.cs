using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Configurações de Velocidade e Tempo")]
    [Tooltip("Tempo em segundos para percorrer 1 tile no grid")]
    public float stepDuration = 0.20f;
    [Tooltip("Tempo em segundos para transição entre andares com pulo")]
    public float jumpDuration = 0.28f;

    [Header("Efeitos Visuais de Fluidez (Game Juice)")]
    [Tooltip("Altura do pequeno salto/bobbing a cada passo normal")]
    public float stepHopHeight = 0.07f;
    [Tooltip("Altura do arco parabólico em saltos de desnível/andar")]
    public float jumpHeight = 0.55f;
    [Tooltip("Ângulo máximo de inclinação na direção do movimento")]
    public float tiltMaxAngle = 4.0f;
    [Tooltip("Intensidade do efeito de squash e stretch")]
    public float squashIntensity = 0.08f;

    [Header("Debug")]
    public bool teste;
    public List<Vector3Int> path;

    // Callbacks de evento
    public event Action OnTraversalStarted;
    public event Action<Vector3Int> OnStepReached;
    public event Action OnTraversalCompleted;

    private SpriteRenderer SR;
    private Transform jumper;
    private Transform spriteTransform;
    private TileLogic tileAtual;
    private Unit unit;

    private Vector3 jumperBaseLocalPos;
    private Vector3 spriteBaseLocalScale;
    private Quaternion spriteBaseLocalRot;
    private bool isMoving = false;

    public bool IsMoving => isMoving;

    void Awake()
    {
        jumper = transform.Find("Jumper");
        if (jumper == null) jumper = transform.Find("jumpe");
        if (jumper == null) jumper = transform;

        SR = GetComponentInChildren<SpriteRenderer>();
        if (SR != null)
        {
            spriteTransform = SR.transform;
            spriteBaseLocalScale = spriteTransform.localScale;
            spriteBaseLocalRot = spriteTransform.localRotation;
        }
        else
        {
            spriteBaseLocalScale = Vector3.one;
            spriteBaseLocalRot = Quaternion.identity;
        }

        if (jumper != null && jumper != transform)
        {
            jumperBaseLocalPos = jumper.localPosition;
        }
        else
        {
            jumperBaseLocalPos = Vector3.zero;
        }

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

    /// <summary>
    /// Percorre a lista de waypoints do caminho de forma fluida, contínua e expressiva.
    /// </summary>
    public IEnumerator Traverse(List<Vector3Int> pathList)
    {
        if (pathList == null || pathList.Count == 0) yield break;

        isMoving = true;
        OnTraversalStarted?.Invoke();

        tileAtual = Board.GetTile(pathList[0]);
        if (tileAtual != null)
        {
            transform.position = tileAtual.worldPos;
            if (unit != null) unit.PlaceAtTile(tileAtual);
        }

        int totalSteps = pathList.Count - 1;

        for (int i = 1; i < pathList.Count; i++)
        {
            TileLogic to = Board.GetTile(pathList[i]);
            if (to == null) continue;

            int stepIndex = i - 1;
            bool isFirstStep = (stepIndex == 0);
            bool isLastStep = (stepIndex == totalSteps - 1);

            // 1. Atualiza a orientação da unidade para a direção do passo
            Vector3Int stepDir = to.pos - tileAtual.pos;
            if (unit != null)
            {
                FacingDirection moveDir = DirectionUtils.VectorToDirection(stepDir);
                unit.SetFacing(moveDir);
            }

            if (tileAtual != null)
            {
                tileAtual.content = null;
            }

            // 2. Determina se é passo normal ou pulo de desnível
            bool isElevationChange = (tileAtual != null && to.floor != null && tileAtual.floor != null && tileAtual.floor != to.floor);

            if (isElevationChange)
            {
                yield return ExecuteJumpStep(to, stepDir);
            }
            else
            {
                yield return ExecuteWalkStep(to, stepDir, isFirstStep, isLastStep, totalSteps);
            }

            OnStepReached?.Invoke(to.pos);
        }

        // Finalização e recuperação elástica
        ResetVisualTransforms();

        if (unit != null && tileAtual != null)
        {
            unit.PlaceAtTile(tileAtual);
            unit.hasMoved = true;
        }

        isMoving = false;
        OnTraversalCompleted?.Invoke();
    }

    /// <summary>
    /// Passo de caminhada contínuo com aceleração inteligente, bobbing vertical, squash/stretch e inclinação.
    /// </summary>
    private IEnumerator ExecuteWalkStep(TileLogic to, Vector3Int stepDir, bool isFirstStep, bool isLastStep, int totalSteps)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = to.worldPos;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, stepDuration);

        tileAtual = to;

        // Determina o ângulo de inclinação (tilt) baseado na direção horizontal
        float targetTilt = 0f;
        if (stepDir.x > 0 || stepDir.y > 0)
        {
            targetTilt = -tiltMaxAngle;
        }
        else if (stepDir.x < 0 || stepDir.y < 0)
        {
            targetTilt = tiltMaxAngle;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);

            // Curva de Easing contínua para trajetórias de múltiplos passos:
            // - Primeiro passo: suave aceleração (EaseIn)
            // - Último passo: suave desaceleração (EaseOut)
            // - Passos intermediários ou passo único: velocidade equilibrada com transição fluida
            float t;
            if (totalSteps == 1)
            {
                t = Mathf.SmoothStep(0f, 1f, rawT);
            }
            else if (isFirstStep)
            {
                t = Mathf.Pow(rawT, 1.4f); // Ease-in
            }
            else if (isLastStep)
            {
                t = 1f - Mathf.Pow(1f - rawT, 1.4f); // Ease-out
            }
            else
            {
                t = rawT; // Movimento contínuo e veloz entre waypoints
            }

            // Deslocamento da posição base
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // 1. Bobbing Vertical (salto do passo com curva senoidal)
            float hop = Mathf.Sin(rawT * Mathf.PI) * stepHopHeight;
            if (jumper != null && jumper != transform)
            {
                jumper.localPosition = jumperBaseLocalPos + new Vector3(0, hop, 0);
            }

            // 2. Squash & Stretch procedural do passo
            if (spriteTransform != null)
            {
                // No meio do passo estica levemente para cima; no impacto (final) achata levemente
                float stretchY = Mathf.Sin(rawT * Mathf.PI) * squashIntensity;
                float squashX = stretchY * 0.5f;

                spriteTransform.localScale = new Vector3(
                    spriteBaseLocalScale.x * (1f - squashX),
                    spriteBaseLocalScale.y * (1f + stretchY),
                    spriteBaseLocalScale.z
                );

                // 3. Inclinação na direção do movimento (Lean)
                float currentTilt = Mathf.Sin(rawT * Mathf.PI) * targetTilt;
                spriteTransform.localRotation = Quaternion.Euler(0, 0, currentTilt);
            }

            // Atualização do sortingOrder no ponto médio do tile
            if (rawT >= 0.45f && SR != null)
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

        // Se for o último passo, pequena recuperação elástica suave
        if (isLastStep)
        {
            yield return SettleLandingAnimation();
        }
    }

    /// <summary>
    /// Pulo parabólico com antecipação no impulso e compressão elástica no pouso.
    /// </summary>
    private IEnumerator ExecuteJumpStep(TileLogic to, Vector3Int stepDir)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = to.worldPos;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, jumpDuration);

        tileAtual = to;

        // Antecipação rápida (pre-jump crouch)
        if (spriteTransform != null)
        {
            spriteTransform.localScale = new Vector3(
                spriteBaseLocalScale.x * 1.12f,
                spriteBaseLocalScale.y * 0.88f,
                spriteBaseLocalScale.z
            );
        }
        yield return new WaitForSeconds(0.03f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float t = Mathf.SmoothStep(0f, 1f, rawT);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            // Arco parabólico suave
            float arc = Mathf.Sin(rawT * Mathf.PI) * jumpHeight;
            if (jumper != null && jumper != transform)
            {
                jumper.localPosition = jumperBaseLocalPos + new Vector3(0, arc, 0);
            }

            // Stretch no ápice do salto
            if (spriteTransform != null)
            {
                float airStretch = Mathf.Sin(rawT * Mathf.PI) * (squashIntensity * 1.8f);
                spriteTransform.localScale = new Vector3(
                    spriteBaseLocalScale.x * (1f - airStretch * 0.5f),
                    spriteBaseLocalScale.y * (1f + airStretch),
                    spriteBaseLocalScale.z
                );
            }

            if (rawT >= 0.45f && SR != null)
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

        // Impacto e amortecimento no pouso do pulo
        yield return SettleLandingAnimation(isJumpLanding: true);
    }

    /// <summary>
    /// Amortecimento e retorno elástico à escala e rotação originais.
    /// </summary>
    private IEnumerator SettleLandingAnimation(bool isJumpLanding = false)
    {
        float landingSquash = isJumpLanding ? squashIntensity * 1.6f : squashIntensity * 0.8f;
        float settleDuration = isJumpLanding ? 0.08f : 0.05f;
        float elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settleDuration;

            // Curva de amortecimento (impacto -> retorno)
            float squash = Mathf.Sin(t * Mathf.PI) * landingSquash;

            if (spriteTransform != null)
            {
                spriteTransform.localScale = new Vector3(
                    spriteBaseLocalScale.x * (1f + squash),
                    spriteBaseLocalScale.y * (1f - squash),
                    spriteBaseLocalScale.z
                );
                spriteTransform.localRotation = Quaternion.Slerp(spriteTransform.localRotation, spriteBaseLocalRot, t);
            }

            if (jumper != null && jumper != transform)
            {
                jumper.localPosition = Vector3.Lerp(jumper.localPosition, jumperBaseLocalPos, t);
            }

            yield return null;
        }

        ResetVisualTransforms();
    }

    private void ResetVisualTransforms()
    {
        if (jumper != null && jumper != transform)
        {
            jumper.localPosition = jumperBaseLocalPos;
        }
        if (spriteTransform != null)
        {
            spriteTransform.localScale = spriteBaseLocalScale;
            spriteTransform.localRotation = spriteBaseLocalRot;
        }
    }

    void OnDisable()
    {
        ResetVisualTransforms();
        isMoving = false;
    }
}