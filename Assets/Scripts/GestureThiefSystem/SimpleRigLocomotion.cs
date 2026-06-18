// SimpleRigLocomotion.cs
// Moves & turns the XR Origin rig. New Input System.
// Sources (any of them work):
//   * Keyboard: WASD/arrows move, Q/E or Left/Right arrows turn, Shift sprint
//   * Gamepad: left stick move, right stick turn
//   * XR controllers: left stick move, right stick snap-ish continuous turn
// Head look in VR is handled by the camera's TrackedPoseDriver (always on).

using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace GestureThiefSystem
{
    public class SimpleRigLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float sprintMultiplier = 2f;

        [Header("Turning")]
        [SerializeField] private float turnSpeed = 90f;

        [SerializeField] private Transform cameraTransform;

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            Vector2 move = Vector2.zero;
            float turn = 0f;
            bool sprint = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed)                               move.x -= 1f;
                if (kb.dKey.isPressed)                               move.x += 1f;
                if (kb.leftArrowKey.isPressed)                       turn  -= 1f;
                if (kb.rightArrowKey.isPressed)                      turn  += 1f;
                if (kb.qKey.isPressed)                               turn  -= 1f;
                if (kb.eKey.isPressed)                               turn  += 1f;
                if (kb.leftShiftKey.isPressed)                       sprint = true;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 ls = gp.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f) move += ls;
                turn += gp.rightStick.ReadValue().x;
                if (gp.leftStickButton.isPressed) sprint = true;
            }

            // XR controllers: scan all devices for thumbstick controls.
            ReadXRSticks(ref move, ref turn);

            if (Mathf.Abs(turn) > 0.01f)
                transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);

            if (move.sqrMagnitude > 0.01f)
            {
                Vector3 fwd   = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right   : transform.right;
                fwd.y = 0f; right.y = 0f; fwd.Normalize(); right.Normalize();
                Vector3 dir = (fwd * move.y + right * move.x);
                if (dir.sqrMagnitude > 1f) dir.Normalize();
                float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);
                transform.position += dir * speed * Time.deltaTime;
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadXRSticks(ref Vector2 move, ref float turn)
        {
            foreach (var dev in InputSystem.devices)
            {
                string lay = dev.layout != null ? dev.layout.ToLower() : "";
                string nm  = dev.name != null ? dev.name.ToLower() : "";
                bool isXR = lay.Contains("xr") || nm.Contains("oculus") || nm.Contains("quest") ||
                            nm.Contains("touch") || nm.Contains("controller");
                if (!isXR) continue;

                var stick = dev.TryGetChildControl("thumbstick") as Vector2Control
                          ?? dev.TryGetChildControl("primary2DAxis") as Vector2Control
                          ?? dev.TryGetChildControl("joystick") as Vector2Control;
                if (stick == null) continue;

                Vector2 v = stick.ReadValue();
                if (v.sqrMagnitude < 0.04f) continue;

                bool isRight = nm.Contains("right") || lay.Contains("right");
                if (isRight) turn += v.x;     // right stick turns
                else         move += v;       // left stick moves
            }
        }
#endif
    }
}