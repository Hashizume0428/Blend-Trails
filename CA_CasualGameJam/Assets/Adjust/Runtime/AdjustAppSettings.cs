using UnityEngine;

namespace AdjustSdk
{
    [CreateAssetMenu(fileName = "AdjustAppSettings", menuName = "Adjust/App Settings")]
    public sealed class AdjustAppSettings : ScriptableObject
    {
        private const string ResourcesPath = "AdjustAppSettings";

        [SerializeField]
        private string appToken;

        public string AppToken => appToken;
        public AdjustEnvironment Environment => AdjustEnvironment.Production;
        public AdjustLogLevel LogLevel => AdjustLogLevel.Info;
        public bool AutoStart => true;
        public bool IsValid => !string.IsNullOrWhiteSpace(appToken);

        public static AdjustAppSettings Load()
        {
            return Resources.Load<AdjustAppSettings>(ResourcesPath);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void CreateIfNeeded()
        {
            if (Load() != null)
            {
                return;
            }

            const string resourcesDirectory = "Assets/Adjust/Resources";
            const string assetPath = resourcesDirectory + "/AdjustAppSettings.asset";
            System.IO.Directory.CreateDirectory(resourcesDirectory);

            var instance = CreateInstance<AdjustAppSettings>();
            UnityEditor.AssetDatabase.CreateAsset(instance, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
