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
    [SerializeField]
    private Image fillBar;

    private float remainingTime;
    private float totalDuration;
    private bool hasTimer;

    public void Initialize(string displayName, Sprite icon, float duration)
    {
        nameText.text = displayName;
        iconImage.sprite = icon;
        hasTimer = duration > 0f;
        remainingTime = duration;
        totalDuration = duration;
        countdownText.gameObject.SetActive(hasTimer);
        fillBar.gameObject.SetActive(hasTimer);
        UpdateCountdownText();
        UpdateFillBar();
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
        UpdateFillBar();
    }

    private void UpdateCountdownText()
    {
        if (hasTimer)
        {
            countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }

    private void UpdateFillBar()
    {
        if (hasTimer && totalDuration > 0f)
        {
            fillBar.fillAmount = remainingTime / totalDuration;
        }
    }
}
