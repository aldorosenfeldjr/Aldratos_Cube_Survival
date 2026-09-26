using System.Collections;
using UnityEngine;

/// <summary>Drops a gem pickup every few seconds. One gem look for every level.</summary>
public class GemSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject gemPrefab;

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
            var config = GameConfig.Instance;
            yield return new WaitForSeconds(Random.Range(config.GemMinInterval, config.GemMaxInterval));

            var x = Random.Range(config.SpawnMinX, config.SpawnMaxX);
            var gem = Instantiate(gemPrefab, new Vector3(x, config.SpawnHeight, 0), Quaternion.identity);
            gem.GetComponent<Rigidbody>().linearDamping = Random.Range(config.PickupMinDrag, config.PickupMaxDrag);
        }
    }
}
