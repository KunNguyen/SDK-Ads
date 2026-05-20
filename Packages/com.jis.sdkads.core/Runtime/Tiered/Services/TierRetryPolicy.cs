using System;
using System.Collections.Generic;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Services
{
    public static class TierRetryPolicy
    {
        static readonly float[] HighDelays = { 2f, 4f, 8f, 16f };
        static readonly float[] MidDelays = { 4f, 8f, 16f };
        static readonly float[] LowDelays = { 8f, 16f, 30f };

        public static float GetDelay(AdTier tier, int retryCount)
        {
            var index = Mathf.Max(0, retryCount - 1);
            var delays = tier switch
            {
                AdTier.High => HighDelays,
                AdTier.Mid => MidDelays,
                AdTier.Low => LowDelays,
                _ => HighDelays
            };

            if (index >= delays.Length)
                index = delays.Length - 1;

            return Mathf.Min(delays[index], 30f);
        }
    }
}
