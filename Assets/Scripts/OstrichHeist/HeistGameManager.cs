// HeistGameManager.cs
// Central game flow (spec section 14). Listens for win/lose events, spawns the
// correct end-screen IN FRONT OF the player (VR-friendly), and locks the game.
// NPC scripts never touch UI — they only raise events; this manager owns the UI.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OstrichHeist
{
    public class HeistGameManager : MonoBehaviour
    {
        public enum Flow { Running, Victory, GameOver }

        [Header("End Screen Canvas (world-space)")]
        [Tooltip("The parent world-space Canvas that holds both screens.")]
        [SerializeField] private Transform endCanvas;
        [SerializeField] private GameObject victoryScreen;   // WinScreen child
        [SerializeField] private GameObject gameOverScreen;  // LoseScreen child

        [Header("Placement")]
        [SerializeField] private float distance = 2.0f;
        [SerializeField] private float worldScale = 0.0025f;
        [SerializeField] private float eyeHeight = 1.5f;

        public Flow State { get; private set; } = Flow.Running;

        private void Awake()
        {
            // NOTE: We intentionally do NOT call GameEvents.Reset() here.
            // Each component manages its own subscribe (OnEnable) / unsubscribe (OnDisable),
            // and a global Reset on Awake would wipe subscriptions made by components whose
            // OnEnable ran earlier (e.g. the Signaler), breaking their event handling.
        }

        private void OnEnable()
        {
            GameEvents.OnSafeZoneReached += HandleVictory;
            GameEvents.OnThiefDefeated   += HandleGameOver;
        }
        private void OnDisable()
        {
            GameEvents.OnSafeZoneReached -= HandleVictory;
            GameEvents.OnThiefDefeated   -= HandleGameOver;
        }

        private void Start()
        {
            if (victoryScreen  != null) victoryScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
        }

        private void HandleVictory()
        {
            if (State != Flow.Running) return;
            State = Flow.Victory;
            Debug.Log("[GameManager] VICTORY");
            ShowScreen(victoryScreen);
        }

        private void HandleGameOver()
        {
            if (State != Flow.Running) return;
            State = Flow.GameOver;
            Debug.Log("[GameManager] GAME OVER");
            ShowScreen(gameOverScreen);
        }

        private void ShowScreen(GameObject screen)
        {
            if (screen == null) return;
            // Move the whole canvas in front of the player, then show only this screen.
            if (endCanvas != null) PositionInFrontOfPlayer(endCanvas);
            if (victoryScreen != null) victoryScreen.SetActive(screen == victoryScreen);
            if (gameOverScreen != null) gameOverScreen.SetActive(screen == gameOverScreen);
        }

        // Spawn directly in front of wherever the player looks, facing them.
        private void PositionInFrontOfPlayer(Transform t)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 basePos = cam.transform.position;
            t.position = basePos + fwd * distance;
            // Face the player (canvas forward points away from camera)
            t.rotation = Quaternion.LookRotation(t.position - basePos, Vector3.up);
            t.localScale = Vector3.one * worldScale;
        }

        // Hook this to the Try Again button.
        public void Restart()
        {
            Time.timeScale = 1f;
            GameEvents.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
