using UnityEditor;
using UnityEngine;

namespace AdjustSdk
{
    public static class AdjustSettingsMenu
    {
        [MenuItem("Adjust/Settings", priority = 0)]
        private static void SelectSettings()
        {
            var settings = AdjustAppSettings.Load();
            if (settings == null)
            {
                Debug.LogError("[Adjust]: Settings asset was not found.");
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
