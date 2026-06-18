// GameOverScreen.cs
// Positions a world-space game-over canvas directly in front of the player's
// camera whenever it becomes visible, so in VR it always appears where the user
// is looking. Also exposes Restart() for the Try Again button.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace GestureThiefSystem
{
    public class GameOverScreen : MonoBehaviour
    {
        [Tooltip("Distance in front of the camera to place the screen.")]
        [SerializeField] private float distance = 2.0f;
        [Tooltip("World-space canvas scale (small because canvas units are pixels).")]
        [SerializeField] private float worldScale = 0.0025f;

        private void OnEnable()
        {
            PositionInFront();
        }

        private void PositionInFront()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Transform ct = cam.transform;

            // Place in front of the camera at eye height, facing the player.
            Vector3 fwd = ct.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            transform.position = ct.position + fwd * distance;
            transform.rotation = Quaternion.LookRotation(transform.position - ct.position);
            transform.localScale = Vector3.one * worldScale;
        }

        // Hooked to the Try Again button.
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
