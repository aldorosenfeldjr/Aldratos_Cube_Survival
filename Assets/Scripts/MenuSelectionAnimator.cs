using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelectionAnimator : MonoBehaviour
{
    [SerializeField]
    private Vector3 pulseScale = new Vector3(1.2f, 1.2f, 1f);
    [SerializeField]
    private float pulseDuration = 0.5f;

    private GameObject animatedSelection;

    private void Update()
    {
        var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (current == animatedSelection)
        {
            return;
        }

        if (animatedSelection != null)
        {
            LeanTween.cancel(animatedSelection);
            animatedSelection.transform.localScale = Vector3.one;
        }

        animatedSelection = current;

        if (animatedSelection != null)
        {
            LeanTween.scale(animatedSelection, pulseScale, pulseDuration).setLoopPingPong();
        }
    }
}
