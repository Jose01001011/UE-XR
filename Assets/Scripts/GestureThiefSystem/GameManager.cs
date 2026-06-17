// GameManager.cs
// Central game loop manager.
// Handles win (egg reached) and lose (thief detected) conditions.
// Attach to a persistent GameObject in the scene.

using UnityEngine;
using UnityEngine.Events;

namespace GestureThiefSystem
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThiefController thief;

        [Header("UI")]
        [SerializeField] private GameObject winScreen;
        [SerializeField] private GameObject loseScreen;
        [SerializeField] private GameObject hudCanvas;

        [Header("Events")]
        public UnityEvent OnGameWon;
        public UnityEvent OnGameLost;

        private bool _gameOver = false;

        // -- Called by ThiefController UnityEvents --

        public void HandleEggReached()
        {
            if (_gameOver) return;
            _gameOver = true;

            Debug.Log("[GameManager] WIN! Egg stolen.");
            if (winScreen  != null) winScreen.SetActive(true);
            if (hudCanvas  != null) hudCanvas.SetActive(false);
            OnGameWon?.Invoke();
        }

        public void HandleThiefDetected()
        {
            if (_gameOver) return;
            _gameOver = true;

            Debug.Log("[GameManager] LOSE! Thief detected.");
            if (loseScreen != null) loseScreen.SetActive(true);
            if (hudCanvas  != null) hudCanvas.SetActive(false);
            OnGameLost?.Invoke();
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
