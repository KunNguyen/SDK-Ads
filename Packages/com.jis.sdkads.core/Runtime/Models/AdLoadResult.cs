namespace JisSDKAds.Core.Models
{
    public struct AdLoadResult
    {
        public bool Success;
        public string ErrorMessage;
        public AdFormat Format;

        public static AdLoadResult Ok(AdFormat format) => new AdLoadResult { Success = true, Format = format };
        public static AdLoadResult Fail(AdFormat format, string error) => new AdLoadResult { Success = false, Format = format, ErrorMessage = error };
    }
}
