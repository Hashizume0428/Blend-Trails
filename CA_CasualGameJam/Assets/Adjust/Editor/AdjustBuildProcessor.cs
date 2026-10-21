using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AdjustSdk
{
    public sealed class AdjustBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android &&
                report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            var settings = AdjustAppSettings.Load();
            if (settings == null || !settings.IsValid)
            {
                throw new BuildFailedException(
                    "[Adjust]: App Key is not configured. Open Adjust > Settings.");
            }
        }
    }
}
