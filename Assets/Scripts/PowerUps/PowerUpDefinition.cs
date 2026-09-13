using UnityEngine;

public abstract class PowerUpDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    [Tooltip("0 = no timer; the effect is removed by being consumed instead (e.g. Shield).")]
    private float duration;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public float Duration => duration;

    public abstract IPowerUpEffect CreateEffect();
}
