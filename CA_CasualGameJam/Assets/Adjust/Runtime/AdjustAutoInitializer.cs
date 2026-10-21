using UnityEngine;

namespace AdjustSdk
{
    public static class AdjustAutoInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var settings = AdjustAppSettings.Load();
            if (settings == null)
            {
                return;
            }

            if (!settings.IsValid)
            {
                Debug.LogWarning("[Adjust]: App Key is not configured. Open Adjust > Settings.");
                return;
            }

            var config = new AdjustConfig(settings.AppToken, settings.Environment)
            {
                LogLevel = settings.LogLevel
            };
            Adjust.InitSdk(config);
        }
    }
}
