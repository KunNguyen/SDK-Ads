#if UNITY_EDITOR
using UnityEditor;

namespace JisSDKAds.Hub
{
    internal static class JisSDKHubDialogs
    {
        public static bool ConfirmRemove(string itemTitle, string details)
        {
            var message =
                $"Remove {itemTitle} from this project?\n\n" +
                details +
                "\n\nThis edits Packages/manifest.json and scripting defines. Unity will resolve packages again.";

            return EditorUtility.DisplayDialog(
                "JIS SDK Hub — Confirm remove",
                message,
                "Remove",
                "Cancel");
        }

        public static bool ConfirmDisableMediation(string mediationTitle, string details) =>
            EditorUtility.DisplayDialog(
                "JIS SDK Hub — Confirm remove",
                $"Remove {mediationTitle} from this project?\n\n{details}",
                "Remove",
                "Cancel");
    }
}
#endif
