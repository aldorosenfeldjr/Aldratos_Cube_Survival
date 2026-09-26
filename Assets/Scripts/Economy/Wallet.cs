using System;

/// <summary>The gem balance. Backed by the save; every change is written to disk.</summary>
public static class Wallet
{
    public static event Action<int> Changed;

    public static int Balance => SaveService.Data.gems;

    public static void Add(int gems)
    {
        if (gems <= 0)
        {
            return;
        }
        SaveService.Data.gems += gems;
        SaveService.Save();
        Changed?.Invoke(Balance);
    }

    public static bool TrySpend(int gems)
    {
        if (gems < 0 || gems > Balance)
        {
            return false;
        }
        SaveService.Data.gems -= gems;
        SaveService.Save();
        Changed?.Invoke(Balance);
        return true;
    }
}
