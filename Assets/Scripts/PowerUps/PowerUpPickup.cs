using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [SerializeField]
    private PowerUpDefinition definition;

    private void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        PowerUpManager.Instance.Grant(definition);
        Destroy(gameObject);
    }
}
