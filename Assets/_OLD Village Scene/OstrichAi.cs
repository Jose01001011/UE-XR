using UnityEngine;
using System.Collections;

public class OstrichAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform thiefTransform;
    [SerializeField] private SignallerAnimator signallerAI;
    [SerializeField] private Transform eggTransform;

    [Header("Movement Settings")]
    [SerializeField] private float wanderSpeed = 2.0f;
    [SerializeField] private float chaseSpeed = 5.0f;
    [SerializeField] private float wanderRadius = 8.0f;
    [SerializeField] private float waitTimeAtPosition = 2.0f;

    private Vector3 targetWanderPoint;
    private bool isThiefRunning = false;
    private bool isWandering = false;

    void Start()
    {
        // Start the random movement cycle
        StartCoroutine(WanderRoutine());
    }

    void Update()
    {
        // 1. Check if the thief has successfully triggered the escape run
        // (We can check the distance or a state, but let's assume if the egg moves or thief gets too close)
        if (thiefTransform != null && Vector3.Distance(thiefTransform.position, eggTransform.position) < 0.5f)
        {
            isThiefRunning = true;
        }

        // 2. Action State Machine
        if (isThiefRunning)
        {
            // CHASE MODE
            StopAllCoroutines(); // Stop wandering immediately
            isWandering = false;
            ChaseTarget(thiefTransform.position);
        }
        else if (isWandering)
        {
            // WANDER MODE
            MoveTowards(targetWanderPoint, wanderSpeed);
        }
    }

    IEnumerator WanderRoutine()
    {
        while (!isThiefRunning)
        {
            isWandering = true;
            // Pick a random spot around the egg's nest to patrol
            targetWanderPoint = GetRandomPointAround(eggTransform.position, wanderRadius);

            // Wait until the ostrich closely reaches that random spot
            while (Vector3.Distance(transform.position, targetWanderPoint) > 0.5f)
            {
                yield return null;
            }

            // Arrived! Pause for a few seconds before picking a new spot
            isWandering = false;
            yield return new WaitForSeconds(waitTimeAtPosition);
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 targetGround = new Vector3(target.x, transform.position.y, target.z);
        transform.LookAt(targetGround);
        transform.position = Vector3.MoveTowards(transform.position, targetGround, speed * Time.deltaTime);
    }

    private void ChaseTarget(Vector3 target)
    {
        MoveTowards(target, chaseSpeed);
    }

    private Vector3 GetRandomPointAround(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);
    }
}