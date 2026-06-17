// ThiefHitReaction.cs
// Flashes the thief red when hit by the ostrich, counts hits, and fires a
// game-over event after a set number of hits (default 4).
// Uses a MaterialPropertyBlock so it tints WITHOUT creating material instances
// and restores the original texture afterwards.

using System.Collections;
using UnityEngine;

namespace GestureThiefSystem
{
    public class ThiefHitReaction : MonoBehaviour
    {
        [Header("Hits")]
        [SerializeField] private int hitsToGameOver = 4;

        [Header("Flash")]
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.25f;
        [Tooltip("Leave empty to auto-collect all child renderers.")]
        [SerializeField] private Renderer[] renderers;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnHit;
        public UnityEngine.Events.UnityEvent OnGameOver;

        private int _hits = 0;
        private bool _flashing = false;
        private MaterialPropertyBlock _mpb;
        private MaterialPropertyBlock _empty;

        public bool IsDown { get { return _hits >= hitsToGameOver; } }
        public int Hits { get { return _hits; } }

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            _empty = new MaterialPropertyBlock();
        }

        /// <summary>Called by OstrichAttack on each landed hit.</summary>
        public void TakeHit()
        {
            if (IsDown) return;
            _hits++;
            if (OnHit != null) OnHit.Invoke();
            Debug.Log("[Thief] Hit " + _hits + "/" + hitsToGameOver);

            if (!_flashing) StartCoroutine(Flash());

            if (_hits >= hitsToGameOver)
            {
                Debug.Log("[Thief] " + hitsToGameOver + " hits -> GAME OVER");
                if (OnGameOver != null) OnGameOver.Invoke();
            }
        }

        public void ResetHits()
        {
            _hits = 0;
            ApplyColor(false);
        }

        private IEnumerator Flash()
        {
            _flashing = true;
            ApplyColor(true);
            yield return new WaitForSeconds(flashDuration);
            ApplyColor(false);
            _flashing = false;
        }

        private void ApplyColor(bool red)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (red)
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_Color", flashColor);
                    _mpb.SetColor("_BaseColor", flashColor);
                    r.SetPropertyBlock(_mpb);
                }
                else
                {
                    r.SetPropertyBlock(_empty); // clears the tint, restores texture
                }
            }
        }
    }
}
