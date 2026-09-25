using System.Collections;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject[] pickupPrefabs;

    private Coroutine spawnCoroutine;

    public void BeginSpawning()
    {
        StopSpawning();
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void ApplyTheme(LevelTheme theme)
    {
        pickupPrefabs = new[] { theme.SpeedBoostPrefab, theme.InvincibilityPrefab, theme.ShieldPrefab };
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            var config = GameConfig.Instance;
            yield return new WaitForSeconds(Random.Range(config.PickupMinInterval, config.PickupMaxInterval));

            if (pickupPrefabs.Length == 0)
            {
                continue;
            }

            var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
            var x = Random.Range(config.SpawnMinX, config.SpawnMaxX);
            var pickup = Instantiate(prefab, new Vector3(x, config.SpawnHeight, 0), Quaternion.identity);

            var rb = pickup.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = Random.Range(config.PickupMinDrag, config.PickupMaxDrag);
            }
        }
    }
}
