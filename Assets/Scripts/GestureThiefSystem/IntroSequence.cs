// IntroSequence.cs
// Orchestrates the opening: the player spawns in an intro area near the
// thief-intro / sig-intro characters, walks forward, and when they reach the
// trigger distance the screen fades to black, the player is moved to the
// gameplay start, the intro characters are hidden, the gameplay actors are
// enabled, and the screen fades back in — then the player guides the thief.
//
// Single-scene approach (no scene load needed): intro and gameplay live in the
// same scene at different positions.

using System.Collections;
using UnityEngine;

namespace GestureThiefSystem
{
    public class IntroSequence : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Transform playerRig;
        [SerializeField] private Vector3 introSpawn   = new Vector3(47f, 0f, -20f);
        [SerializeField] private Vector3 gameplayStart = new Vector3(0f, 0f, -12f);

        [Header("Trigger")]
        [Tooltip("World point the player walks toward to trigger the transition.")]
        [SerializeField] private Vector3 triggerPoint = new Vector3(47f, 0f, -12f);
        [Tooltip("How close (metres, horizontal) the player must get to trigger.")]
        [SerializeField] private float triggerRadius = 2.5f;

        [Header("Scene Objects")]
        [Tooltip("Intro characters shown during the walk-in, hidden after transition.")]
        [SerializeField] private GameObject[] introObjects;
        [Tooltip("Gameplay actors enabled only after the transition.")]
        [SerializeField] private GameObject[] gameplayObjects;

        [Header("Refs")]
        [SerializeField] private ScreenFader fader;

        private bool _triggered;
        private bool _gameplayActive;

        [Header("Mode")]
        [Tooltip("If false, skip the walking intro and start directly in gameplay.")]
        [SerializeField] private bool playIntro = false;

        private void Start()
        {
            if (!playIntro)
            {
                // Intro disabled: start directly in gameplay.
                // IMPORTANT: do NOT move the player — they start exactly where the
                // XR Origin is placed in the editor.
                SetActiveAll(introObjects, false);   // hide intro characters
                SetGameplayEnabled(true);            // gameplay active immediately
                _triggered = true;
                _gameplayActive = true;
                return;
            }

            // --- Intro enabled path ---
            if (playerRig != null)
            {
                playerRig.position = introSpawn;
                playerRig.rotation = Quaternion.identity;
            }
            SetActiveAll(introObjects, true);
            SetGameplayEnabled(false);
        }

        private void Update()
        {
            if (_triggered || playerRig == null) return;

            Vector3 a = playerRig.position; a.y = 0f;
            Vector3 b = triggerPoint;       b.y = 0f;
            if (Vector3.Distance(a, b) <= triggerRadius)
            {
                _triggered = true;
                StartCoroutine(DoTransition());
            }
        }

        private IEnumerator DoTransition()
        {
            // Fade to black
            if (fader != null) yield return fader.FadeOut();
            else yield return new WaitForSeconds(0.2f);

            // Swap world: hide intro, move player to gameplay start, enable gameplay
            SetActiveAll(introObjects, false);
            if (playerRig != null)
            {
                playerRig.position = gameplayStart;
                playerRig.rotation = Quaternion.identity;
            }
            SetGameplayEnabled(true);
            _gameplayActive = true;

            // Small beat in black, then fade in to reveal the guiding gameplay
            yield return new WaitForSeconds(0.4f);
            if (fader != null) yield return fader.FadeIn();
        }

        private void SetActiveAll(GameObject[] arr, bool on)
        {
            if (arr == null) return;
            foreach (var go in arr) if (go != null) go.SetActive(on);
        }

        // Enable/disable the gameplay actor behaviour so they hold still during intro.
        private void SetGameplayEnabled(bool on)
        {
            if (gameplayObjects == null) return;
            foreach (var go in gameplayObjects)
            {
                if (go == null) continue;
                go.SetActive(true); // keep visible
                foreach (var mb in go.GetComponents<MonoBehaviour>())
                {
                    // Don't toggle this script or transforms; just AI/controllers.
                    if (mb is IntroSequence) continue;
                    mb.enabled = on;
                }
                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh) agent.isStopped = !on;
            }
        }

        public bool GameplayActive => _gameplayActive;
    }
}
