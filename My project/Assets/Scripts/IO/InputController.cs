using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public delegate void DelegateModel(object sender, object args);

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    [Header("Configurações de DAS (Delayed Auto Shift)")]
    [Tooltip("Atraso inicial antes de iniciar a rolagem contínua ao segurar uma direção")]
    public float initialRepeatDelay = 0.22f;
    [Tooltip("Intervalo entre passos durante a rolagem contínua no grid")]
    public float repeatInterval = 0.075f;

    public DelegateModel OnMove;
    public DelegateModel OnFire;

    // Estado do eixo Horizontal
    private int lastH = 0;
    private float hHoldTimer = 0f;
    private float hNextRepeatTime = 0f;

    // Estado do eixo Vertical
    private int lastV = 0;
    private float vHoldTimer = 0f;
    private float vNextRepeatTime = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        int h = 0;
        int v = 0;
        bool fire1 = false;
        bool fire2 = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1;

            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                fire1 = true;
            }
            if (kb.xKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.leftAltKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            {
                fire2 = true;
            }
        }

        if (gp != null)
        {
            if (gp.dpad.left.isPressed || gp.leftStick.left.isPressed) h -= 1;
            if (gp.dpad.right.isPressed || gp.leftStick.right.isPressed) h += 1;
            if (gp.dpad.down.isPressed || gp.leftStick.down.isPressed) v -= 1;
            if (gp.dpad.up.isPressed || gp.leftStick.up.isPressed) v += 1;

            if (gp.buttonSouth.wasPressedThisFrame) fire1 = true;
            if (gp.buttonEast.wasPressedThisFrame) fire2 = true;
        }
#endif

        Vector3Int moved = Vector3Int.zero;
        moved.x = ProcessAxisInput(h, ref lastH, ref hHoldTimer, ref hNextRepeatTime);
        moved.y = ProcessAxisInput(v, ref lastV, ref vHoldTimer, ref vNextRepeatTime);

        if (moved != Vector3Int.zero && OnMove != null)
        {
            OnMove(this, moved);
        }

        if (fire1 && OnFire != null)
        {
            OnFire(this, 1);
        }
        if (fire2 && OnFire != null)
        {
            OnFire(this, 2);
        }
    }

    /// <summary>
    /// Processa o input de um eixo com DAS: resposta imediata no 1º frame, pausa inicial e rolagem rápida subsequente.
    /// </summary>
    private int ProcessAxisInput(int currentInput, ref int lastInput, ref float holdTimer, ref float nextRepeatTime)
    {
        if (currentInput == 0)
        {
            lastInput = 0;
            holdTimer = 0f;
            nextRepeatTime = 0f;
            return 0;
        }

        int sign = Math.Sign(currentInput);

        // Se mudou de direção ou acabou de pressionar
        if (sign != lastInput)
        {
            lastInput = sign;
            holdTimer = Time.time;
            nextRepeatTime = Time.time + initialRepeatDelay;
            return sign;
        }

        // Se está segurando a mesma direção
        if (Time.time >= nextRepeatTime)
        {
            nextRepeatTime = Time.time + repeatInterval;
            return sign;
        }

        return 0;
    }
}

