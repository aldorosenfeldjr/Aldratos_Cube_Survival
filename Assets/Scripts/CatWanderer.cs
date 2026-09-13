using UnityEngine;

public class CatWanderer : MonoBehaviour
{
    [SerializeField]
    private float minX = -7f;
    [SerializeField]
    private float maxX = 7f;
    [SerializeField]
    private float wanderZRange = 1.5f;

    [SerializeField]
    private float walkSpeed = 0.6f;
    [SerializeField]
    private float runSpeed = 1.6f;
    [SerializeField]
    private float turnSpeed = 6f;
    [SerializeField]
    [Range(0f, 1f)]
    private float runChance = 0.2f;

    [SerializeField]
    private float idleMinTime = 2f;
    [SerializeField]
    private float idleMaxTime = 5f;
    [SerializeField]
    private float arriveThreshold = 0.15f;

    [SerializeField]
    private float hazardAvoidRadius = 1.2f;
    [SerializeField]
    private string hazardTag = "Hazard";

    private enum State { Idle, Walking, Running }

    private Animator animator;
    private State state;
    private float stateTimer;
    private float centerZ;
    private Vector3 destination;
    private float currentSpeed;
    private const float CrossFadeTime = 0.2f;

    private void Start()
    {
        animator = GetComponent<Animator>();
        centerZ = transform.position.z;
        EnterIdle();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    PickDestination();
                }
                break;

            case State.Walking:
            case State.Running:
                MoveTowardsDestination();
                break;
        }
    }

    private void EnterIdle()
    {
        state = State.Idle;
        stateTimer = Random.Range(idleMinTime, idleMaxTime);
        animator.CrossFade(Random.value < 0.5f ? "Idle_A" : "Idle_B", CrossFadeTime);
    }

    private void PickDestination()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var candidate = new Vector3(
                Random.Range(minX, maxX),
                transform.position.y,
                centerZ + Random.Range(-wanderZRange, wanderZRange));

            if (!IsNearHazard(candidate))
            {
                destination = candidate;
                BeginMoving();
                return;
            }
        }

        // Couldn't find a clear spot; just wait a bit and try again next frame.
        stateTimer = 0.5f;
    }

    private void BeginMoving()
    {
        bool run = Random.value < runChance;
        state = run ? State.Running : State.Walking;
        currentSpeed = run ? runSpeed : walkSpeed;
        animator.CrossFade(run ? "Run" : "Walk", CrossFadeTime);
    }

    private void MoveTowardsDestination()
    {
        if (IsNearHazard(transform.position))
        {
            EnterIdle();
            stateTimer = 0.3f;
            return;
        }

        var toDestination = destination - transform.position;
        toDestination.y = 0f;
        var distance = toDestination.magnitude;

        if (distance <= arriveThreshold)
        {
            EnterIdle();
            return;
        }

        var direction = toDestination / distance;
        transform.position += direction * (currentSpeed * Time.deltaTime);

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private bool IsNearHazard(Vector3 position)
    {
        foreach (var hazard in GameObject.FindGameObjectsWithTag(hazardTag))
        {
            if (Vector3.Distance(position, hazard.transform.position) < hazardAvoidRadius)
            {
                return true;
            }
        }
        return false;
    }
}
