using System.Collections;
using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private int maxHazardsToSpawn = 3;
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
            var hazardsToSpawn = Random.Range(1, maxHazardsToSpawn);

            for (int i = 0; i < hazardsToSpawn; i++)
            {
                var x = Random.Range(-7, 7);
                var drag = Random.Range(maxDrag, minDrag);

                var hazard = Instantiate(hazardPrefab, new Vector3(x, 11, 0), Quaternion.identity);
                hazard.GetComponent<Rigidbody>().linearDamping = drag;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
