using UnityEngine;

/// <summary>
/// Puts the selected companion in the scene, where this object stands, and swaps it when the selection changes.
/// Replaces the hand-placed cat: the cat is now just the free default companion.
/// </summary>
public class CompanionSpawner : MonoBehaviour
{
    private GameObject current;

    public GameObject Current => current;
    public CompanionDefinition Definition { get; private set; }

    private void OnEnable()
    {
        UnlockService.Changed += Spawn;
        Spawn();
    }

    private void OnDisable()
    {
        UnlockService.Changed -= Spawn;
    }

    public void Spawn()
    {
        var definition = UnlockService.Selected(UnlockCategory.Companion) as CompanionDefinition;
        if (definition == Definition && current != null)
        {
            return;
        }

        if (current != null)
        {
            Destroy(current);
            current = null;
        }

        Definition = definition;
        if (definition == null || definition.Prefab == null)
        {
            return;
        }

        current = Instantiate(definition.Prefab, transform.position, transform.rotation);
        current.name = "Companion_" + definition.Id;
        // Sized and seated with its feet on the spawner's height on the first rendered frame (see CompanionFitter).
        CompanionFitter.OnGround(current, definition.WorldSize, transform.position.y);

        if (definition.Wanders)
        {
            current.AddComponent<CatWanderer>();
        }
    }
}
