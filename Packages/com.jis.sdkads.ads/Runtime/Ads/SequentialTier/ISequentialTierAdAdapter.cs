using System;

namespace JisSDKAds.Ads.SequentialTier
{
    public struct SequentialTierShowError
    {
        public int Code;
        public string Message;
    }

    public struct SequentialTierPaidEvent
    {
        public double Revenue;
        public string Currency;
        public int Precision;
        public string AdUnitId;
        public string AdSource;
    }

    public struct SequentialTierShowHooks
    {
        public Action onClosed;
        public Action onOpened;
        public Action<SequentialTierShowError?> onFailed;
        public Action<SequentialTierPaidEvent> onPaid;
        public Action onRewardGranted;
    }

    public interface ISequentialTierAdAdapter
    {
        bool IsReady { get; }
        void Destroy();
        void Load(string adUnitId, int loadGeneration, int expectedGeneration,
            Action onSuccess, Action<int, string> onFail);
        void RegisterShowCallbacks(SequentialTierShowHooks hooks);
        bool TryShow();
    }
}
