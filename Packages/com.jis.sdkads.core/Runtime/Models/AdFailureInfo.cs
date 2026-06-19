namespace JisSDKAds.Core.Models
{
    public readonly struct AdFailureInfo
    {
        public AdFormat Format { get; }
        public string ProviderId { get; }
        public AdFailureReason Reason { get; }
        public string Message { get; }

        public AdFailureInfo(AdFormat format, string providerId, AdFailureReason reason, string message)
        {
            Format = format;
            ProviderId = providerId ?? string.Empty;
            Reason = reason;
            Message = message ?? string.Empty;
        }

        public static AdFailureInfo ForAd(AdFormat format, string providerId, string message) =>
            new AdFailureInfo(format, providerId, AdFailureClassifier.Classify(message), message);

        public static AdFailureInfo ForProvider(string providerId, string message) =>
            new AdFailureInfo(default, providerId, AdFailureClassifier.Classify(message), message);
    }
}
