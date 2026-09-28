using TMPro;
using UnityEngine;

/// <summary>Shows the wallet balance next to a coin icon and keeps it current. One prefab, used on the main menu and the selection screens.</summary>
public class GemCounter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    private void OnEnable()
    {
        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }
        Wallet.Changed += Show;
        Show(Wallet.Balance);
    }

    private void OnDisable()
    {
        Wallet.Changed -= Show;
    }

    private void Show(int balance)
    {
        label.text = balance.ToString();
    }
}
