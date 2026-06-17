using UnityEngine;

public class SignallerAnimator : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform ostrichTarget; // Drag the Ostrich object here
    [SerializeField] private Transform eggTarget;     // Drag the Egg object here

    [Header("Distance Thresholds (Meters from Egg)")]
    [Tooltip("Distance from egg where the signaler notices the ostrich and enters 'Looking' state.")]
    [SerializeField] private float lookDistance = 25.0f;
    
    [Tooltip("Distance from egg where danger is critical; signaler enters 'Waving' state to warn the thief.")]
    [SerializeField] private float waveDistance = 8.0f;

    [Header("Animation Stability")]
    [Tooltip("How many seconds the signaller will force-continue waving after the ostrich leaves the zone.")]
    [SerializeField] private float waveStayActiveDuration = 3.0f;

    private Animator animator;
    private float waveTimer = 0f; // Tracks how much waving time is left

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (ostrichTarget == null)
        {
            Debug.LogError("SignallerStealthAI: Please assign the Ostrich Target in the Inspector!");
        }

        if (eggTarget == null)
        {
            Debug.LogError("SignallerStealthAI: Please assign the Egg Target in the Inspector!");
        }
    }

    void Update()
    {
        if (ostrichTarget == null || eggTarget == null || animator == null) return;

        // 1. Calculate direct distance between the EGG and the Ostrich
        float distanceToEgg = Vector3.Distance(eggTarget.position, ostrichTarget.position);

        // 2. Tick down the wave safety timer over real-time
        if (waveTimer > 0f)
        {
            waveTimer -= Time.deltaTime;
        }

        // 3. If the ostrich enters the danger zone, reset/refresh the waving timer back to max
        if (distanceToEgg <= waveDistance)
        {
            waveTimer = waveStayActiveDuration;
        }

        // 4. State Logic Machine
        // The signaller waves if the ostrich is directly in the zone OR if our countdown timer is still running
        if (distanceToEgg <= waveDistance || waveTimer > 0f)
        {
            // CRITICAL ZONE: Wave violently to warn the boy!
            SetStealthState(look: true, wave: true);
        }
        else if (distanceToEgg <= lookDistance)
        {
            // WARNING ZONE: Ostrich is approaching the egg area, track it intently.
            SetStealthState(look: true, wave: false);
        }
        else
        {
            // SAFE ZONE: Ostrich is completely gone. Relax into idle.
            SetStealthState(look: false, wave: false);
        }
    }

    /// <summary>
    /// Helper method to cleanly pass state booleans to the Unity Animator
    /// </summary>
    private void SetStealthState(bool look, bool wave)
    {
        animator.SetBool("IsLooking", look);
        animator.SetBool("IsWaving", wave);
    }

    private void OnDrawGizmosSelected()
    {
        if (eggTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(eggTarget.position, waveDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eggTarget.position, lookDistance);
        }
    }

    public bool IsWavingActive()
    {
        if (animator != null)
        {
            return animator.GetBool("IsWaving");
        }
        return false;
    }
}