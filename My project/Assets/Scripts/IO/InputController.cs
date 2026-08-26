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
    float hCooldown = 0;
    float vCooldown = 0;
    float cooldownTime = 0.22f;

    public static InputController Instance;
    public DelegateModel OnMove;
    public DelegateModel OnFire;

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

        if (h != 0) { moved.x = GetMoved(ref hCooldown, Math.Sign(h)); } else { hCooldown = 0; }
        if (v != 0) { moved.y = GetMoved(ref vCooldown, Math.Sign(v)); } else { vCooldown = 0; }

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

    int GetMoved(ref float cooldownSum, int value)
    {
        if (Time.time > cooldownSum)
        {
            cooldownSum = Time.time + cooldownTime;
            return value;
        }
        return 0;
    }
}
