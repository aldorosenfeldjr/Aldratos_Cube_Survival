using UnityEngine;

/// <summary>A falling gem. Spins while falling; the player catching it credits the run, anything else removes it.</summary>
public class GemPickup : MonoBehaviour
{
    [SerializeField]
    private float spinDegreesPerSecond = 180f;

    private bool collected;

    private void Update()
    {
        transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collected && collision.gameObject.GetComponent<Player>() != null)
        {
            collected = true;
            GameManager.Instance.CollectGem();
        }

        // Hazards don't count as a miss (same as power-up crates); anything else ends the gem.
        if (collected || !collision.gameObject.CompareTag("Hazard"))
        {
            Destroy(gameObject);
        }
    }
}
