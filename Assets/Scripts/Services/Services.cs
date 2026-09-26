/// <summary>
/// Where game code finds the ad and store services. The Editor defaults to fakes (so trials and purchases can be
/// tried and tested); every other build defaults to "unavailable". Mobile adapters replace them at startup
/// (economy step 5). The UI decides what to show from each service's <c>IsAvailable</c>, never from platform checks.
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
#if UNITY_EDITOR
        return new FakeAdService();
#else
        return new NullAdService();
#endif
    }

    private static IStoreService CreateDefaultStore()
    {
#if UNITY_EDITOR
        return new FakeStoreService();
#else
        return new NullStoreService();
#endif
    }
}
