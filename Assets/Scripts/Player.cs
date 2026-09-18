using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField]
    private float forceMultiplier = 3f;
    [SerializeField]
    private float maximumVelocity = 3f;
    [SerializeField]
    private float jumpForce = 6f;
    [SerializeField]
    private float fallGravityMultiplier = 2.5f;
    [SerializeField]
    private ParticleSystem deathParticles;

    private Rigidbody rb;
    private Unity.Cinemachine.CinemachineImpulseSource cinemachineImpulseSource;
    private bool isGrounded;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cinemachineImpulseSource = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
    }

    // Update is called once per frame
    void Update()
    {   
        if (GameManager.Instance == null)
        {
            return;
        }

        float horizontalInput = 0;

        if (Input.GetMouseButton(0))
        {
            var center = Screen.width / 2;
            var mousePosition = Input.mousePosition;
            if (mousePosition.x > center)
            {
                horizontalInput = 1;
            }
            else if (mousePosition.x < center)
            {
                horizontalInput = -1;
            }
        }
        else
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }
        
        var speedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.SpeedMultiplier : 1f;

        if (rb.linearVelocity.magnitude <= maximumVelocity * speedMultiplier)
        {
            rb.AddForce(new Vector3(horizontalInput * forceMultiplier * speedMultiplier * Time.deltaTime, 0, 0));
        }

        if (isGrounded && Time.timeScale > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
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
            cinemachineImpulseSource.GenerateImpulse();
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
