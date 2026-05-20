using System;

namespace JisSDKAds.Core.Tiered.Models
{
    [Serializable]
    public class TierAdUnit
    {
        public AdTier Tier;
        public string UnitId;
        public bool IsLoaded;
        public bool IsLoading;
        public int RetryCount;
        public int FailCount;
        public int SuccessCount;
        public float AverageResponseTime;
        public long LastLoadedTime;
        public long LastSuccessTime;
        public float FillRate;
        public DateTime TemporaryDisabledUntil = DateTime.MinValue;
        public bool IsTemporarilyDisabled;

        public bool IsDisabled => IsTemporarilyDisabled && DateTime.UtcNow < TemporaryDisabledUntil;

        public bool CanAttemptLoad => !string.IsNullOrEmpty(UnitId) && !IsLoading && !IsDisabled;

        public void MarkLoadStarted()
        {
            IsLoading = true;
        }

        public void MarkLoadSuccess(float responseTimeMs)
        {
            IsLoading = false;
            IsLoaded = true;
            RetryCount = 0;
            SuccessCount++;
            LastLoadedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LastSuccessTime = LastLoadedTime;
            UpdateAverageResponseTime(responseTimeMs);
            UpdateFillRate(success: true);
        }

        public void MarkLoadFailed()
        {
            IsLoading = false;
            IsLoaded = false;
            RetryCount++;
            FailCount++;
            UpdateFillRate(success: false);
        }

        public void MarkConsumed()
        {
            IsLoaded = false;
        }

        public void DisableTemporarily(float durationSeconds)
        {
            IsTemporarilyDisabled = true;
            TemporaryDisabledUntil = DateTime.UtcNow.AddSeconds(durationSeconds);
        }

        public void ClearTemporaryDisable()
        {
            IsTemporarilyDisabled = false;
            TemporaryDisabledUntil = DateTime.MinValue;
        }

        void UpdateAverageResponseTime(float responseTimeMs)
        {
            if (SuccessCount <= 1)
                AverageResponseTime = responseTimeMs;
            else
                AverageResponseTime = ((AverageResponseTime * (SuccessCount - 1)) + responseTimeMs) / SuccessCount;
        }

        void UpdateFillRate(bool success)
        {
            var attempts = SuccessCount + FailCount;
            FillRate = attempts > 0 ? (float)SuccessCount / attempts * 100f : 0f;
        }
    }
}
