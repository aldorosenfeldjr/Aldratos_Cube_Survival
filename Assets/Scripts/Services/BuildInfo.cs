using UnityEngine;

/// <summary>
/// What kind of build this is. Test builds (the Editor, development builds, and any build with the <c>TEST_BUILD</c> scripting
/// symbol, currently set for WebGL) use fake ads/store and give a fresh save starting gems, so everything can be tried on a device
/// before the real adapters exist. A real release build has neither.
/// </summary>
public static class BuildInfo
{
    /// <summary>Fake ad and store services instead of "unavailable".</summary>
    public static bool UsesTestServices
    {
        get
        {
#if UNITY_EDITOR || TEST_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }

    /// <summary>A fresh save starts with gems. Never in the Editor, so unit tests and normal Editor play are unaffected.</summary>
    public static bool GivesStartingGems
    {
        get
        {
            if (Application.isEditor)
            {
                return false;
            }
#if TEST_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }
}
