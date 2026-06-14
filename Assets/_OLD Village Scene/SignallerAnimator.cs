using UnityEngine;

public class SignallerAnimator : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform ostrichTarget; // Drag the Ostrich object here

    [Header("Distance Thresholds (Meters)")]
    [Tooltip("Distance where the signaler notices the ostrich and enters 'Looking' state.")]
    [SerializeField] private float lookDistance = 25.0f;
    
    [Tooltip("Distance where danger is critical; signaler enters 'Waving' state to warn the thief.")]
    [SerializeField] private float waveDistance = 8.0f;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (ostrichTarget == null)
        {
            Debug.LogError("SignallerStealthAI: Please assign the Ostrich Target in the Inspector!");
        }
    }

    void Update()
    {
        if (ostrichTarget == null || animator == null) return;

        // Calculate direct distance between Signaller and the Ostrich
        float distanceToOstrich = Vector3.Distance(transform.position, ostrichTarget.position);

        // State Logic Machine based on proximity
        if (distanceToOstrich <= waveDistance)
        {
            // CRITICAL ZONE: Ostrich is very close. Wave violently to warn the boy.
            SetStealthState(look: true, wave: true);
        }
        else if (distanceToOstrich <= lookDistance)
        {
            // WARNING ZONE: Ostrich is in range, track it intently.
            SetStealthState(look: true, wave: false);
        }
        else
        {
            // SAFE ZONE: Ostrich is far away. Relax into idle.
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

    // Visualizes the zones in the Unity Scene view for easy debugging/balancing!
    private void OnDrawGizmosSelected()
    {
        // Red wire sphere for critical warning zone (Waving)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, waveDistance);

        // Yellow wire sphere for caution zone (Looking)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lookDistance);
    }

    public bool IsWavingActive()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            // This checks if the "IsWaving" boolean in your Signaller's Animator is currently true
            return anim.GetBool("IsWaving");
        }
        return false;
    }
}
