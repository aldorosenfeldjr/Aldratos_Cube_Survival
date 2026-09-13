using UnityEngine;

public class CatWanderer : MonoBehaviour
{
    [SerializeField]
    private float minX = -5.3f;
    [SerializeField]
    private float maxX = 5.3f;

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
    private float hazardAvoidRadius = 1.5f;
    [SerializeField]
    private string hazardTag = "Hazard";

    [SerializeField]
    private LayerMask groundMask = ~0;
    [SerializeField]
    private float groundCheckHeight = 5f;

    private enum State { Idle, Walking, Running }

    private Animator animator;
    private State state;
    private float stateTimer;
    private float fixedZ;
    private Vector3 destination;
    private float currentSpeed;
    private const float CrossFadeTime = 0.2f;

    private void Start()
    {
        animator = GetComponent<Animator>();
        fixedZ = transform.position.z;
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
        for (int attempt = 0; attempt < 8; attempt++)
        {
            var x = Random.Range(minX, maxX);

            if (!TryGetGroundHeight(x, fixedZ, out var groundY))
            {
                continue; // no solid ground here (a gap) - try another spot
            }

            var candidate = new Vector3(x, groundY, fixedZ);
            if (!IsNearHazard(candidate))
            {
                destination = candidate;
                BeginMoving();
                return;
            }
        }

        // Couldn't find a clear, solid spot; wait and try again shortly.
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
        var nextPosition = transform.position + direction * (currentSpeed * Time.deltaTime);

        // Keep the cat glued to the actual ground height as it moves, since the
        // terrain isn't flat across X.
        if (TryGetGroundHeight(nextPosition.x, nextPosition.z, out var groundY))
        {
            nextPosition.y = groundY;
        }

        transform.position = nextPosition;

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private bool TryGetGroundHeight(float x, float z, out float groundY)
    {
        var origin = new Vector3(x, transform.position.y + groundCheckHeight, z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, groundCheckHeight * 2f, groundMask))
        {
            groundY = hit.point.y;
            return true;
        }
        groundY = 0f;
        return false;
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
