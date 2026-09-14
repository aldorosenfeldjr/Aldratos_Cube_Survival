// Assets/Scripts/PowerUps/PowerUpHUDIcon.cs
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUDIcon : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TMPro.TextMeshProUGUI countdownText;
    [SerializeField]
    private TMPro.TextMeshProUGUI nameText;

    private float remainingTime;
    private bool hasTimer;

    public void Initialize(string displayName, Sprite icon, float duration)
    {
        nameText.text = displayName;
        iconImage.sprite = icon;
        hasTimer = duration > 0f;
        remainingTime = duration;
        countdownText.gameObject.SetActive(hasTimer);
        UpdateCountdownText();
    }

    private void Update()
    {
        if (!hasTimer)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f)
        {
            remainingTime = 0f;
        }
        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (hasTimer)
        {
            countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }
}
