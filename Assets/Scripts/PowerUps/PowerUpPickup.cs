using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [SerializeField]
    private PowerUpDefinition definition;

    private bool grantedToPlayer;

    private void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            // Destroy() is deferred to end of frame, so a same-frame floor
            // contact processed before this one may have already called
            // Destroy() below without granting - this still wins the race
            // since grantedToPlayer wasn't set by that branch.
            if (!grantedToPlayer)
            {
                grantedToPlayer = true;
                PowerUpManager.Instance.Grant(definition);
            }
            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.CompareTag("Hazard"))
        {
            // Crates are too frequent/random to count as a "miss" trigger.
            return;
        }

        Destroy(gameObject);
    }
}
