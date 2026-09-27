using UnityEngine;

/// <summary>
/// Where game code finds the ad and store services. The Editor, development builds and test builds (see <see cref="BuildInfo"/>) default to fakes (so trials and
/// purchases can be tried on a device before the real adapters exist); release builds default to "unavailable".
/// Mobile adapters replace them at startup (economy step 5). The UI decides what to show from each service's <c>IsAvailable</c>, never from platform checks.
/// </summary>
public static class Services
{
    private static IAdService ads;
    private static IStoreService store;

    public static IAdService Ads
    {
        get => ads ?? (ads = CreateDefaultAds());
        set => ads = value;
    }

    public static IStoreService Store
    {
        get => store ?? (store = CreateDefaultStore());
        set => store = value;
    }

    private static IAdService CreateDefaultAds()
    {
        return BuildInfo.UsesTestServices ? (IAdService)new FakeAdService() : new NullAdService();
    }

    private static IStoreService CreateDefaultStore()
    {
        return BuildInfo.UsesTestServices ? (IStoreService)new FakeStoreService() : new NullStoreService();
    }
}
