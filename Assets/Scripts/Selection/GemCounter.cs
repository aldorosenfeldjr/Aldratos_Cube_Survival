using TMPro;
using UnityEngine;

/// <summary>Shows the wallet balance and keeps it current. One prefab, used on the main menu and the selection screen.</summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class GemCounter : MonoBehaviour
{
    private TextMeshProUGUI label;

    private void OnEnable()
    {
        label = GetComponent<TextMeshProUGUI>();
        Wallet.Changed += Show;
        Show(Wallet.Balance);
    }

    private void OnDisable()
    {
        Wallet.Changed -= Show;
    }

    private void Show(int balance)
    {
        label.text = $"Gems: {balance}";
    }
}
