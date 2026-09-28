using UnityEngine;

/// <summary>
/// A companion animal. The look is a prefab (model + Animator with Idle_A / Idle_B, and Walk / Run when it can move).
/// The prefab is scaled at runtime so its largest dimension equals <see cref="WorldSize"/>, so sizes are set per row in
/// one place and never depend on how a model was authored.
/// </summary>
[CreateAssetMenu(fileName = "Companion", menuName = "Game/Companion Definition")]
public class CompanionDefinition : UnlockableDefinition
{
    private const float PreviewSize = 2.0f;
    private const float PreviewYaw = 145f;

    [SerializeField] private GameObject prefab;
    [Tooltip("Largest dimension of the companion in the world, in units (the player box is 1).")]
    [SerializeField] private float worldSize = 0.8f;
    [Tooltip("Has Walk and Run animations, so it wanders. Otherwise it stays where it is and idles.")]
    [SerializeField] private bool wanders;
    [SerializeField] private Color tileColor = Color.white;

    public GameObject Prefab => prefab;
    public float WorldSize => worldSize;
    public bool Wanders => wanders;

    public override Color TileColor => tileColor;

    /// <summary>Scales <paramref name="instance"/> so its largest bounds dimension equals <paramref name="size"/>.</summary>
    public static void FitToSize(GameObject instance, float size)
    {
        var extent = MaxExtent(instance);
        if (extent > 0.0001f)
        {
            instance.transform.localScale *= size / extent;
        }
    }

    public static float MaxExtent(GameObject instance)
    {
        var bounds = WorldBounds(instance);
        return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    }

    public static Bounds WorldBounds(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        var bounds = new Bounds(instance.transform.position, Vector3.zero);
        var first = true;
        foreach (var renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer)
            {
                continue;
            }
            var rendererBounds = renderer.bounds;
            if (first)
            {
                bounds = rendererBounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(rendererBounds);
            }
        }
        return bounds;
    }

    /// <summary>A live 3/4 view: the animal turned toward the preview camera, idling (or walking in place), centred and fitted.</summary>
    public override GameObject CreatePreview(Transform parent)
    {
        var instance = Instantiate(prefab, parent);
        instance.name = "Preview_" + Id;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(0f, PreviewYaw, 0f);

        // Loops its own idle (and, for wanderers, an in-place walk); CompanionFitter then sizes and centres what is shown.
        var animator = instance.GetComponent<Animator>();
        if (animator != null)
        {
            instance.AddComponent<PreviewIdleLoop>().Begin(animator, wanders);
        }
        CompanionFitter.OnPedestal(instance, PreviewSize, parent.position + Vector3.up * PreviewFloor);
        return instance;
    }
}
