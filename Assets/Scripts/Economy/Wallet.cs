using System;

/// <summary>The coin balance. Backed by the save; every change is written to disk.</summary>
public static class Wallet
{
    public static event Action<int> Changed;

    public static int Balance => SaveService.Data.coins;

    public static void Add(int coins)
    {
        if (coins <= 0)
        {
            return;
        }
        SaveService.Data.coins += coins;
        SaveService.Save();
        Changed?.Invoke(Balance);
    }

    public static bool TrySpend(int coins)
    {
        if (coins < 0 || coins > Balance)
        {
            return false;
        }
        SaveService.Data.coins -= coins;
        SaveService.Save();
        Changed?.Invoke(Balance);
        return true;
    }
}
