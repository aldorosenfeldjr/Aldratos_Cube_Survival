using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem deathParticles;

    private Rigidbody rb;
    private bool isGrounded;
    private float keyboardSteer;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {   
        if (GameManager.Instance == null)
        {
            return;
        }

        float horizontalInput = 0;
        var config = GameConfig.Instance;
        UpdateKeyboardSteer(config.KeyboardSteerRamp);

        // Pointer covers mouse and touch: hold on the left/right half of the screen to steer.
        var pointer = Pointer.current;
        if (pointer != null && pointer.press.isPressed)
        {
            var center = Screen.width / 2;
            var pointerX = pointer.position.ReadValue().x;
            if (pointerX > center)
            {
                horizontalInput = 1;
            }
            else if (pointerX < center)
            {
                horizontalInput = -1;
            }
        }
        else
        {
            var stick = Gamepad.current != null ? Gamepad.current.leftStick.x.ReadValue() : 0f;
            horizontalInput = Mathf.Abs(stick) > Mathf.Abs(keyboardSteer) ? stick : keyboardSteer;
        }

        var speedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.SpeedMultiplier : 1f;

        if (rb.linearVelocity.magnitude <= config.MaxSpeed * speedMultiplier)
        {
            rb.AddForce(new Vector3(horizontalInput * config.MoveForce * speedMultiplier * Time.deltaTime, 0, 0));
        }

        var jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (isGrounded && Time.timeScale > 0 && jumpPressed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, config.JumpForce, rb.linearVelocity.z);
        }
    }

    /// <summary>
    /// Arrow keys / A-D steering that ramps toward the pressed direction instead of jumping to it,
    /// and snaps to 0 when the direction reverses (same feel as the old Input Manager axis).
    /// </summary>
    private void UpdateKeyboardSteer(float ramp)
    {
        float target = 0;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) target += 1;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) target -= 1;
        }

        if (target != 0 && Mathf.Sign(target) != Mathf.Sign(keyboardSteer))
        {
            keyboardSteer = 0;
        }
        keyboardSteer = Mathf.MoveTowards(keyboardSteer, target, ramp * Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (GameConfig.Instance.FallGravityMultiplier - 1f), ForceMode.Acceleration);
        }

        isGrounded = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            return;
        }

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void OnEnable()
    {
        ResetState();
    }

    public void ResetState()
    {
        transform.position = new Vector3(0, 0.75f, 0);
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
    }

    private void GameOver()
    {
        if (GameManager.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        GameManager.Instance.GameOver();
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
            {
                Destroy(collision.gameObject);
                return;
            }

            if (PowerUpManager.Instance != null && PowerUpManager.Instance.TryConsumeShield())
            {
                Destroy(collision.gameObject);
                return;
            }

            GameOver();
            Instantiate(deathParticles, transform.position, Quaternion.identity);
            if (CameraShaker.Instance != null)
            {
                CameraShaker.Instance.ShakeDeath();
            }
        }
    }

    private void OnTriggerExit(Collider other) 
    {
        if (other.gameObject.CompareTag("FallDown"))
        {
            GameOver();
        }
    }
}
