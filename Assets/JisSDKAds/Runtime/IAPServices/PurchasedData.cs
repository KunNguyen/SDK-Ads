using System.Collections.Generic;
using UnityEngine;

namespace BanThanh
{
    [System.Serializable]
    public class PurchasedData
    {
        [field: SerializeField] public string ProductId { get; set; }
        [field: SerializeField] public string TransactionId { get; set; }
        [field: SerializeField] public string Receipt { get; set; }
        [field: SerializeField] public string PurchaseToken { get; set; }
        [field: SerializeField] public string PurchaseTime { get; set; }

        public PurchasedData() { }
        public PurchasedData(string productId, string transactionId, string purchaseToken, string receipt, string purchaseTime)
        {
            ProductId = productId;
            TransactionId = transactionId;
            Receipt = receipt;
            PurchaseToken = purchaseToken;
            PurchaseTime = purchaseTime;
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        public static PurchasedData FromJson(string json)
        {
            return JsonUtility.FromJson<PurchasedData>(json);
        }
    }
    
    [System.Serializable]
    public class PurchasedDataList
    {
        [field: SerializeField] public List<PurchasedData> PurchasedItems { get; set; } = new List<PurchasedData>();

        public bool IsPurchased(string productId)
        {
            return PurchasedItems.Exists(item => item.ProductId == productId);
        }

        public void AddPurchasedItem(PurchasedData item)
        {
            if(PurchasedItems.Exists(i => i.ProductId == item.ProductId && i.TransactionId == item.TransactionId))
            {
                return;
            }
            PurchasedItems.Add(item);
        }

        public void RemovePurchasedItem(PurchasedData item)
        {
            if (item == null || !PurchasedItems.Contains(item))
            {
                return;
            }
            PurchasedItems.Remove(item);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static PurchasedDataList FromJson(string json)
        {
            return JsonUtility.FromJson<PurchasedDataList>(json);
        }
    }
}