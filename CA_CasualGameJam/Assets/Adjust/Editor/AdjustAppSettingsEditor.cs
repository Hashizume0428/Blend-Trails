using UnityEditor;
using UnityEngine;

namespace AdjustSdk
{
    [CustomEditor(typeof(AdjustAppSettings))]
    public sealed class AdjustAppSettingsEditor : Editor
    {
        private SerializedProperty appToken;

        private void OnEnable()
        {
            appToken = serializedObject.FindProperty("appToken");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(appToken, new GUIContent("App Key"));
            EditorGUILayout.HelpBox(
                "App Keyを設定すると、Android / iOSアプリ起動時にAdjustが自動で初期化されます。",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
