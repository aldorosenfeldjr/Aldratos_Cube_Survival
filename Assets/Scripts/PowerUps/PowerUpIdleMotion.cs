// Assets/Scripts/PowerUps/PowerUpIdleMotion.cs
using UnityEngine;

public class PowerUpIdleMotion : MonoBehaviour
{
    [SerializeField]
    private float spinSpeedDegreesPerSecond = 90f;
    [SerializeField]
    private float bobAmplitude = 0.08f;
    [SerializeField]
    private float bobSpeed = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeedDegreesPerSecond * Time.deltaTime, 0f, Space.Self);

        var offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }
}
