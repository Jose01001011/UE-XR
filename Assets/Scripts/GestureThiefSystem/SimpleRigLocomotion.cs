// SimpleRigLocomotion.cs
// Moves the XR Origin rig with keyboard (WASD / arrows) and a gamepad/controller
// left stick, using the NEW Input System (this project is New-Input-System-only,
// so legacy Input.GetKey does nothing). Movement is relative to where the camera
// is looking, projected onto the ground plane. In VR, physical room-scale walking
// also works automatically via head tracking — this adds stick/key locomotion on top.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GestureThiefSystem
{
    public class SimpleRigLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Metres per second.")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("Hold for a speed boost.")]
        [SerializeField] private float sprintMultiplier = 2f;

        [Header("Turning (keyboard Q/E or arrow Left/Right)")]
        [SerializeField] private float turnSpeed = 90f;

        [Tooltip("Camera used to determine 'forward'. Auto-finds Main Camera if empty.")]
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
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1f;
                if (kb.aKey.isPressed)                                move.x -= 1f;
                if (kb.dKey.isPressed)                                move.x += 1f;
                if (kb.leftArrowKey.isPressed)                        turn  -= 1f;
                if (kb.rightArrowKey.isPressed)                       turn  += 1f;
                if (kb.qKey.isPressed)                                turn  -= 1f;
                if (kb.eKey.isPressed)                                turn  += 1f;
                if (kb.leftShiftKey.isPressed)                        sprint = true;
            }

            // Gamepad / mapped VR controller left stick + right stick turn
            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 ls = gp.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f) move += ls;
                turn += gp.rightStick.ReadValue().x;
                if (gp.leftStickButton.isPressed) sprint = true;
            }

            // Apply turning (rotate the rig around its own up axis)
            if (Mathf.Abs(turn) > 0.01f)
                transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);

            // Apply movement relative to camera look (flattened to ground)
            if (move.sqrMagnitude > 0.01f)
            {
                Vector3 fwd = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
                fwd.y = 0f; right.y = 0f;
                fwd.Normalize(); right.Normalize();

                Vector3 dir = (fwd * move.y + right * move.x);
                if (dir.sqrMagnitude > 1f) dir.Normalize();

                float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);
                transform.position += dir * speed * Time.deltaTime;
            }
#endif
        }
    }
}
