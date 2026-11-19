using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Light drawing taps and a stronger, single failure pulse. No vibration in the Editor.</summary>
public static class TileHaptics
{
    private static float _lastTap = float.NegativeInfinity;
    private static bool _unavailable;
    private static bool _errorUnavailable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { _lastTap = float.NegativeInfinity; _unavailable = _errorUnavailable = false; }

    public static void LightTap()
    {
        // Coalesce intermediate cells reached by the same fast swipe into a short tap.
        if (_unavailable || Time.unscaledTime - _lastTap < 0.04f) return;
        _lastTap = Time.unscaledTime;
#if UNITY_IOS && !UNITY_EDITOR
        CP_LightTap();
#elif UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                activity.Call("runOnUiThread", new AndroidJavaRunnable(AndroidTap));
        }
        catch (AndroidJavaException exception) { Unavailable(exception.Message); }
#endif
    }

    public static void ErrorTap()
    {
        if (_errorUnavailable) return;
#if UNITY_IOS && !UNITY_EDITOR
        CP_ErrorTap();
#elif UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", 180L, 255))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", 180L);
            }
        }
        catch (AndroidJavaException exception)
        {
            _errorUnavailable = true;
            Debug.LogWarning("Error haptics unavailable: " + exception.Message);
        }
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CP_LightTap();
    [DllImport("__Internal")] private static extern void CP_ErrorTap();
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static void AndroidTap()
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var window = activity.Call<AndroidJavaObject>("getWindow"))
            using (var view = window.Call<AndroidJavaObject>("getDecorView"))
            using (var constants = new AndroidJavaClass("android.view.HapticFeedbackConstants"))
                view.Call<bool>("performHapticFeedback", constants.GetStatic<int>("CLOCK_TICK"));
        }
        catch (AndroidJavaException exception) { Unavailable(exception.Message); }
    }
    private static void Unavailable(string reason)
    {
        if (_unavailable) return;
        _unavailable = true;
        Debug.LogWarning("Tile haptics unavailable: " + reason);
    }
#endif
}
