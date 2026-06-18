// GameManager.cs
// Central game loop: WIN (egg reached) / LOSE (thief beaten down).
// Self-wires by subscribing to ThiefController.OnEggReached and
// ThiefHitReaction.OnGameOver at runtime — no inspector wiring required.

using UnityEngine;
using UnityEngine.Events;

namespace GestureThiefSystem
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;

        [Header("UI")]
        [SerializeField] private GameObject winScreen;
        [SerializeField] private GameObject loseScreen;
        [SerializeField] private GameObject hudCanvas;

        [Header("Events")]
        public UnityEvent OnGameWon;
        public UnityEvent OnGameLost;

        private bool _gameOver = false;

        private void Start()
        {
            // Auto-find if not assigned.
            if (thief == null)    thief    = FindAnyObjectByType<ThiefController>();
            if (thiefHit == null && thief != null)
                thiefHit = thief.GetComponent<ThiefHitReaction>();

            // Subscribe directly — robust regardless of inspector wiring.
            if (thief != null)    thief.OnEggReached.AddListener(HandleEggReached);
            if (thiefHit != null) thiefHit.OnGameOver.AddListener(HandleThiefDetected);

            Debug.Log("[GameManager] Ready. thief=" + (thief!=null) + " thiefHit=" + (thiefHit!=null) +
                      " winScreen=" + (winScreen!=null) + " loseScreen=" + (loseScreen!=null));
        }

        private void OnDestroy()
        {
            if (thief != null)    thief.OnEggReached.RemoveListener(HandleEggReached);
            if (thiefHit != null) thiefHit.OnGameOver.RemoveListener(HandleThiefDetected);
        }

        public void HandleEggReached()
        {
            if (_gameOver) return;
            _gameOver = true;
            Debug.Log("[GameManager] WIN! Egg stolen.");
            if (winScreen != null) winScreen.SetActive(true);
            if (hudCanvas != null) hudCanvas.SetActive(false);
            OnGameWon?.Invoke();
        }

        public void HandleThiefDetected()
        {
            if (_gameOver) return;
            _gameOver = true;
            Debug.Log("[GameManager] LOSE! Thief caught.");
            if (loseScreen != null) loseScreen.SetActive(true);
            if (hudCanvas != null) hudCanvas.SetActive(false);
            OnGameLost?.Invoke();
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}