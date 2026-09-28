using UnityEngine;

/// <summary>
/// Keeps a companion's preview alive: loops Idle_A/Idle_B like <see cref="CatWanderer"/> does at rest, and for
/// companions that can wander, occasionally plays a short in-place Walk cycle too (root motion disabled, so the
/// model never leaves its spot on the preview stage). Replaces freezing the Animator on one still frame.
/// </summary>
public class PreviewIdleLoop : MonoBehaviour
{
    private const float CrossFadeTime = 0.2f;

    [SerializeField] private float idleMinTime = 2f;
    [SerializeField] private float idleMaxTime = 4f;
    [SerializeField] private float walkDuration = 1.5f;

    private Animator animator;
    private bool wanders;
    private float timer;
    private bool walking;

    public void Begin(Animator targetAnimator, bool canWander)
    {
        animator = targetAnimator;
        wanders = canWander;
        animator.applyRootMotion = false;
        animator.speed = 1f;
        PlayIdle();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }
        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }
        if (!walking && wanders && Random.value < 0.5f)
        {
            walking = true;
            timer = walkDuration;
            animator.CrossFade("Walk", CrossFadeTime);
        }
        else
        {
            PlayIdle();
        }
    }

    private void PlayIdle()
    {
        walking = false;
        timer = Random.Range(idleMinTime, idleMaxTime);
        animator.CrossFade(Random.value < 0.5f ? "Idle_A" : "Idle_B", CrossFadeTime);
    }
}
