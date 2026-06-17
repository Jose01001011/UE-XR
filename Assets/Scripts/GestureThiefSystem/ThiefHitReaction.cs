// ThiefHitReaction.cs
// Flashes thief red on hit, counts hits to game over.
// Also notifies ThiefIndicator to flash the plumbob red.

using System.Collections;
using UnityEngine;

namespace GestureThiefSystem
{
    public class ThiefHitReaction : MonoBehaviour
    {
        [Header("Hits")]
        [SerializeField] private int hitsToGameOver = 4;

        [Header("Flash")]
        [SerializeField] private Color flashColor   = Color.red;
        [SerializeField] private float flashDuration = 0.25f;
        [SerializeField] private Renderer[] renderers;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnHit;
        public UnityEngine.Events.UnityEvent OnGameOver;

        private int  _hits     = 0;
        private bool _flashing = false;
        private MaterialPropertyBlock _mpb;
        private MaterialPropertyBlock _empty;
        private ThiefIndicator _indicator;

        public bool IsDown => _hits >= hitsToGameOver;
        public int  Hits   => _hits;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            _mpb   = new MaterialPropertyBlock();
            _empty = new MaterialPropertyBlock();
        }

        private void Start()
        {
            _indicator = FindAnyObjectByType<ThiefIndicator>();
        }

        public void TakeHit()
        {
            if (IsDown) return;
            _hits++;
            _indicator?.FlashHit();   // flash the plumbob red
            OnHit?.Invoke();
            Debug.Log("[Thief] Hit " + _hits + "/" + hitsToGameOver);
            if (!_flashing) StartCoroutine(Flash());
            if (_hits >= hitsToGameOver)
            {
                Debug.Log("[Thief] GAME OVER — " + hitsToGameOver + " hits.");
                OnGameOver?.Invoke();
            }
        }

        public void ResetHits() { _hits = 0; ApplyColor(false); }

        private System.Collections.IEnumerator Flash()
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
                if (red) {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_Color",     flashColor);
                    _mpb.SetColor("_BaseColor", flashColor);
                    r.SetPropertyBlock(_mpb);
                } else {
                    r.SetPropertyBlock(_empty);
                }
            }
        }
    }
}