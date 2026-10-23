//
// AppLovin MAX Unity Plugin C# Wrapper
//

using UnityEngine;

public class MaxSdk :
#if UNITY_EDITOR
    // Check for Unity Editor first since the editor also responds to the currently selected platform.
    MaxSdkUnityEditor
#elif UNITY_ANDROID
    MaxSdkAndroid
#elif UNITY_IPHONE || UNITY_IOS
    MaxSdkiOS
#else
    MaxSdkUnityEditor
#endif
{
    private const string _version = "8.6.5";

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS
    // Keep startup inside the plugin so scenes do not need an initializer component.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        if (IsInitialized()) return;

        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
        InitializeSdk();
    }

    private static void OnSdkInitialized(MaxSdkBase.SdkConfiguration configuration)
    {
        MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
#if UNITY_EDITOR
        Debug.Log("[AppLovin MAX] SDK initialized (Unity Editor simulation)");
#else
        Debug.Log("[AppLovin MAX] SDK initialized");
#endif
    }
#endif

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    public static string Version
    {
        get { return _version; }
    }
}
