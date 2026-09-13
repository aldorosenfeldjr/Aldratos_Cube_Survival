using System.Collections;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject[] pickupPrefabs;
    [SerializeField]
    private float minSpawnInterval = 4f;
    [SerializeField]
    private float maxSpawnInterval = 6f;
    [SerializeField]
    private float spawnMinX = -7f;
    [SerializeField]
    private float spawnMaxX = 7f;
    [SerializeField]
    private float spawnHeight = 11f;
    [SerializeField]
    private float minDrag;
    [SerializeField]
    private float maxDrag;

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

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            if (pickupPrefabs.Length == 0)
            {
                continue;
            }

            var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
            var x = Random.Range(spawnMinX, spawnMaxX);
            var pickup = Instantiate(prefab, new Vector3(x, spawnHeight, 0), Quaternion.identity);

            var rb = pickup.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = Random.Range(minDrag, maxDrag);
            }
        }
    }
}
