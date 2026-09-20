using UnityEngine;

public enum PowerUpImportance
{
    Minor,
    Major,
    Supreme
}

public abstract class PowerUpDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    [Tooltip("0 = no timer; the effect is removed by being consumed instead (e.g. Shield).")]
    private float duration;
    [SerializeField]
    private PowerUpImportance importance = PowerUpImportance.Minor;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public float Duration => duration;
    public PowerUpImportance Importance => importance;

    public abstract IPowerUpEffect CreateEffect();
}
