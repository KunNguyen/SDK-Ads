#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.IAP
{
    static class IapPurchasePersistence
    {
        const string PurchasedDataKey = "jis_iap_purchased_data";
        const string ProcessedTransactionsKey = "jis_iap_processed_txns";

        static HashSet<string> _processedTransactions;

        public static PurchasedDataList LoadPurchasedData()
        {
            var stored = PlayerPrefs.GetString(PurchasedDataKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
                return new PurchasedDataList();

            if (IapLocalDataProtector.TryUnprotect(stored, out var json))
            {
                try
                {
                    return PurchasedDataList.FromJson(json) ?? new PurchasedDataList();
                }
                catch
                {
                    return new PurchasedDataList();
                }
            }

            // Legacy (pre-protection) plain-text entries: migrate them once so paying users
            // don't lose entitlements, then immediately re-save in protected form. Anything
            // that fails both the protected and legacy parse is discarded rather than trusted,
            // since an unreadable/tampered payload should never silently grant entitlement.
            try
            {
                var legacyList = PurchasedDataList.FromJson(stored);
                if (legacyList == null)
                    return new PurchasedDataList();
                SavePurchasedData(legacyList);
                return legacyList;
            }
            catch
            {
                Debug.LogWarning("[IAP] Local entitlement data failed integrity check and could not be read as legacy data — discarding.");
                return new PurchasedDataList();
            }
        }

        public static void SavePurchasedData(PurchasedDataList list)
        {
            PlayerPrefs.SetString(PurchasedDataKey, IapLocalDataProtector.Protect(list.ToJson()));
            PlayerPrefs.Save();
        }

        public static void RecordPurchase(PurchasedData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ProductId))
                return;
            var list = LoadPurchasedData();
            list.AddPurchasedItem(data);
            SavePurchasedData(list);
        }

        public static bool IsPurchased(string productId)
        {
            return !string.IsNullOrEmpty(productId) && LoadPurchasedData().IsPurchased(productId);
        }

        static void EnsureProcessedLoaded()
        {
            if (_processedTransactions != null)
                return;
            _processedTransactions = new HashSet<string>();
            var stored = PlayerPrefs.GetString(ProcessedTransactionsKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
                return;

            var readable = IapLocalDataProtector.TryUnprotect(stored, out var json) ? json : stored;
            try
            {
                var wrapper = JsonUtility.FromJson<TransactionIdList>(readable);
                if (wrapper?.Ids != null)
                {
                    foreach (var id in wrapper.Ids)
                    {
                        if (!string.IsNullOrEmpty(id))
                            _processedTransactions.Add(id);
                    }
                }
            }
            catch
            {
                _processedTransactions = new HashSet<string>();
            }
        }

        public static bool WasTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return false;
            EnsureProcessedLoaded();
            return _processedTransactions.Contains(transactionId);
        }

        public static void MarkTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return;
            EnsureProcessedLoaded();
            if (!_processedTransactions.Add(transactionId))
                return;
            var wrapper = new TransactionIdList { Ids = new List<string>(_processedTransactions) };
            PlayerPrefs.SetString(ProcessedTransactionsKey, IapLocalDataProtector.Protect(JsonUtility.ToJson(wrapper)));
            PlayerPrefs.Save();
        }

        [System.Serializable]
        class TransactionIdList
        {
            public List<string> Ids = new List<string>();
        }
    }
}
#endif
