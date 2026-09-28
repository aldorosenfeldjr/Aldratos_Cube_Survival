using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-to-rotate for a selection preview. Lives on the same RawImage the preview camera renders into.
/// Rotates <see cref="Target"/> — the previewed item itself, not the stage/camera — around world up while
/// the pointer drags across it. Replaces the previous "no rotation" preview behaviour.
/// </summary>
public class PreviewRotator : MonoBehaviour, IDragHandler
{
    [SerializeField] private float degreesPerPixel = 0.3f;

    public Transform Target { get; set; }

    public void OnDrag(PointerEventData eventData)
    {
        if (Target == null)
        {
            return;
        }
        Target.Rotate(Vector3.up, -eventData.delta.x * degreesPerPixel, Space.World);
    }
}
