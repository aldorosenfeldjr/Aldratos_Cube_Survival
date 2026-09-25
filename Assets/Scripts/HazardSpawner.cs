using System.Collections;
using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject hazardPrefab;

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
        hazardPrefab = theme.HazardPrefab;
    }

    public void ClearAll()
    {
        foreach (var hazard in GameObject.FindGameObjectsWithTag("Hazard"))
        {
            Destroy(hazard);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            var config = GameConfig.Instance;
            var hazardsToSpawn = Random.Range(1, config.MaxHazardsPerWave);

            for (int i = 0; i < hazardsToSpawn; i++)
            {
                // Integer range on purpose: hazards land on whole-number columns.
                var x = Random.Range((int)config.SpawnMinX, (int)config.SpawnMaxX);
                var drag = Random.Range(config.HazardMaxDrag, config.HazardMinDrag);

                var hazard = Instantiate(hazardPrefab, new Vector3(x, config.SpawnHeight, 0), Quaternion.identity);
                hazard.GetComponent<Rigidbody>().linearDamping = drag;
            }

            yield return new WaitForSeconds(config.HazardWaveInterval);
        }
    }
}
