using UnityEngine;

/// <summary>
/// Scales a companion to its world size and seats it, once, on its first rendered frame. It waits until the Animator has
/// evaluated its first pose (LateUpdate), because a skinned model measures differently before and after that, and the pose
/// that is measured must be the pose that is shown. The world spawner and the selection preview share this rule.
/// </summary>
public class CompanionFitter : MonoBehaviour
{
    private float size;
    private bool centre;
    private Vector3 target;

    /// <summary>Fit to <paramref name="worldSize"/> and stand with the feet at <paramref name="groundY"/>.</summary>
    public static void OnGround(GameObject companion, float worldSize, float groundY)
    {
        var fitter = companion.AddComponent<CompanionFitter>();
        fitter.size = worldSize;
        fitter.target = new Vector3(0f, groundY, 0f);
    }

    /// <summary>Fit to <paramref name="worldSize"/> and centre the model on <paramref name="centre"/>.</summary>
    public static void CentredOn(GameObject companion, float worldSize, Vector3 centre)
    {
        var fitter = companion.AddComponent<CompanionFitter>();
        fitter.size = worldSize;
        fitter.centre = true;
        fitter.target = centre;
    }

    private void LateUpdate()
    {
        CompanionDefinition.FitToSize(gameObject, size);
        var bounds = CompanionDefinition.WorldBounds(gameObject);
        transform.position += centre ? target - bounds.center : new Vector3(0f, target.y - bounds.min.y, 0f);
        Destroy(this);
    }
}
