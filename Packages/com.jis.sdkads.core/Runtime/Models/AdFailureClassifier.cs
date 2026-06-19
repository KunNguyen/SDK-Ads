namespace JisSDKAds.Core.Models
{
    public static class AdFailureClassifier
    {
        public static AdFailureReason Classify(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return AdFailureReason.Unknown;

            var e = error.ToLowerInvariant();
            if (e.Contains("no fill") || e.Contains("nofill") || e.Contains("no ad") || e.Contains("load failed"))
                return AdFailureReason.NoFill;
            if (e.Contains("network") || e.Contains("internet") || e.Contains("offline") || e.Contains("connection"))
                return AdFailureReason.NetworkError;
            if (e.Contains("timeout") || e.Contains("timed out"))
                return AdFailureReason.Timeout;
            if (e.Contains("not initialized") || e.Contains("not ready"))
                return AdFailureReason.NotInitialized;
            if (e.Contains("not loaded"))
                return AdFailureReason.NotLoaded;

            return AdFailureReason.InternalError;
        }
    }
}
