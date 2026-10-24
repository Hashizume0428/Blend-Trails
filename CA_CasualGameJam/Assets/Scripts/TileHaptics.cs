using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Short device feedback when a tile gains a color. No vibration in the Editor.</summary>
public static class TileHaptics
{
    private static float _lastTap = float.NegativeInfinity;
    private static bool _unavailable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { _lastTap = float.NegativeInfinity; _unavailable = false; }

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

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CP_LightTap();
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
