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
            var json = PlayerPrefs.GetString(PurchasedDataKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new PurchasedDataList();
            try
            {
                return PurchasedDataList.FromJson(json) ?? new PurchasedDataList();
            }
            catch
            {
                return new PurchasedDataList();
            }
        }

        public static void SavePurchasedData(PurchasedDataList list)
        {
            PlayerPrefs.SetString(PurchasedDataKey, list.ToJson());
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
            var json = PlayerPrefs.GetString(ProcessedTransactionsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;
            try
            {
                var wrapper = JsonUtility.FromJson<TransactionIdList>(json);
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
            PlayerPrefs.SetString(ProcessedTransactionsKey, JsonUtility.ToJson(wrapper));
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
