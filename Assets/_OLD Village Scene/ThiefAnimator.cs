using System.Collections;
using UnityEngine;

public class ThiefAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Signaller character here.")]
    [SerializeField] private SignallerAnimator signallerAI; 

    [Tooltip("Drag the Ostrich character here.")]
    [SerializeField] private Transform ostrichTransform;    

    [Tooltip("Drag the Egg item here.")]
    [SerializeField] private Transform eggTarget;            

    [Tooltip("Create an empty GameObject where the thief runs to escape, and drag it here.")]
    [SerializeField] private Transform escapeTarget;         

    [Tooltip("Drag the GameObject with the StealthLevelManager script here.")]
    [SerializeField] private StealthLevelManager levelManager; 

    [Header("Movement Settings")]
    [Tooltip("Speed while crawling or grabbing.")]
    [SerializeField] private float crawlSpeed = 1.2f;

    [Tooltip("Speed when sprinting away with the egg.")]
    [SerializeField] private float runSpeed = 5.0f;

    [Tooltip("Time spent idling before starting to move.")]
    [SerializeField] private float initialDelay = 3.0f;   

    [Tooltip("If the ostrich gets closer than this distance when checking, it's Game Over.")]
    [SerializeField] private float caughtRadius = 0.8f;    // Increased default slightly for easier balancing

    private Animator animator;
    private bool isRunningAway = false;
    private bool isCaught = false;

    // Tracker for the script's internal AI state
    private enum ThiefState { Idle, Crawling, StandingToLook, AdvancingToEgg, Escaping }
#pragma warning disable CS0414
    private ThiefState currentState = ThiefState.Idle;
#pragma warning restore CS0414

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("ThiefStealthAI: No Animator component found on this GameObject!");
            return;
        }

        if (levelManager == null)
        {
            Debug.LogError("ThiefStealthAI: Please assign the Stealth Level Manager in the Inspector!");
        }

        // Begin the stealth sequence loop
        StartCoroutine(ThiefSequenceRoutine());
    }

    IEnumerator ThiefSequenceRoutine()
    {
        // ==========================================
        // STEP 1: INITIAL WAIT & START
        // ==========================================
        yield return new WaitForSeconds(initialDelay);
        if (isCaught) yield break; // Safety check if caught during start delay
        animator.SetTrigger("StartMoving"); // idle -> kneel -> crawl

        bool hasClearance = false;

        // ==========================================
        // STEP 2: THE STOP-AND-GO CRAWL LOOP
        // ==========================================
        while (!hasClearance)
        {
            if (isCaught) yield break;

            currentState = ThiefState.Crawling;
            animator.SetBool("IsSpottedOrPausing", false);
            
            // Crawl forward for 3 seconds before pausing to check the sky
            float crawlTimer = 0f;
            while (crawlTimer < 3.0f)
            {
                if (isCaught) yield break; // Instantly aborts walking if caught frame-by-frame

                MoveTowards(eggTarget.position, crawlSpeed);
                crawlTimer += Time.deltaTime;
                yield return null;
            }

            if (isCaught) yield break;

            // Routine Checkpoint: Stop crawling and stand up to check the Signaller
            currentState = ThiefState.StandingToLook;
            animator.SetBool("IsSpottedOrPausing", true); // crawl -> crouch_stand_1 -> looking

            // Give the thief 3 seconds to play crouch_stand_1 and look around in 'looking'
            yield return new WaitForSeconds(3.0f);

            if (isCaught) yield break;

            // Check what the Signaller is doing right now
            if (signallerAI != null && signallerAI.IsWavingActive())
            {
                // DANGER: Signaller is waving! 
                Debug.Log("Thief: Signaller is waving! Danger detected, looping back to crawl safely.");
                
                // Wait for the kneel animation to return them to the floor before starting the loop again
                yield return new WaitForSeconds(1.5f); 
            }
            else
            {
                // ALL CLEAR: Signaller is calm. Turn off the switch to trigger the 'kneel_1' transition!
                Debug.Log("Thief: Signaller is not waving. All clear! Moving to final approach.");
                animator.SetBool("IsSpottedOrPausing", false); // looking -> kneel_1 -> grabbing
                hasClearance = true;
            }
        }

        // ==========================================
        // STEP 3: THE FINAL APPROACH (GRABBING)
        // ==========================================
        if (isCaught) yield break;
        currentState = ThiefState.AdvancingToEgg;
        
        // Wait for kneel_1 animation to hit the ground completely
        yield return new WaitForSeconds(1.5f);

        // Sneak the final distance until they arrive directly at the egg nest
        while (Vector3.Distance(transform.position, eggTarget.position) > 0.8f)
        {
            if (isCaught) yield break;

            MoveTowards(eggTarget.position, crawlSpeed);
            yield return null;
        }

        // ==========================================
        // STEP 4: THE END GAME (ESCAPE OR FAIL)
        // ==========================================
        if (isCaught) yield break;

        // Double check proximity one final split second before securing victory
        float distanceToOstrich = Vector3.Distance(transform.position, ostrichTransform.position);
        
        if (distanceToOstrich <= caughtRadius)
        {
            TriggerFailure();
            yield break; 
        }
        else
        {
            // Success! Grab the egg and sprint
            animator.SetTrigger("ReachedEgg"); // grabbing -> crouch_stand -> egg_run
            currentState = ThiefState.Escaping;
            isRunningAway = true;
            Debug.Log("SUCCESS: Thief secured the egg! Running away!");
        }
    }

    void Update()
    {
        // ---> REAL-TIME CONTINUOUS THREAT CHECK <---
        // This constantly scans distance every frame regardless of what step the Coroutine loop is running.
        if (!isCaught && ostrichTransform != null)
        {
            float continuousDistance = Vector3.Distance(transform.position, ostrichTransform.position);
            if (continuousDistance <= caughtRadius)
            {
                TriggerFailure();
            }
        }

        // If they successfully stole the egg, continuously sprint toward the escape zone
        if (isRunningAway && !isCaught && escapeTarget != null)
        {
            MoveTowards(escapeTarget.position, runSpeed);
        }
    }

    /// <summary>
    /// Centralized failure process that forces the animator state, kills movement, and handles UI.
    /// </summary>
    private void TriggerFailure()
    {
        isCaught = true;
        isRunningAway = false;
        animator.SetTrigger("WasCaught"); // Breaks layout instantly and goes to Caught_Freeze
        Debug.LogError("GAME OVER: Thief was caught by the Ostrich!");

        if (levelManager != null)
        {
            levelManager.TriggerGameOver();
        }
    }

    /// <summary>
    /// Smoothly rotates the thief to face a target point and moves them forward.
    /// </summary>
    private void MoveTowards(Vector3 targetPosition, float speed)
    {
        // Keeps the thief grounded on their current Y height so they don't float or sink
        Vector3 targetGround = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        
        // Snap/Rotate to look directly at where they are moving
        transform.LookAt(targetGround);
        
        // Translate position forward step-by-step
        transform.position = Vector3.MoveTowards(transform.position, targetGround, speed * Time.deltaTime);
    }

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind(); // Completely flushes and restarts the animator graph
            animator.Update(0f); // Forces it to evaluate frame 1 instantly
        }
        
        // Safety reset to make sure movement loops aren't blocked
        isCaught = false;
        isRunningAway = false;
    }
}