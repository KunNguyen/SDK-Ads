#if UNITY_IAP_ACTIVE
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Purchasing;

namespace JisSDKAds.IAP
{
    [System.Serializable]
    public class IAPPackage
    {
        [field: SerializeField] public string ProductID { get; set; }
        [field: SerializeField] public ProductType ProductType { get; set; }
        [field: SerializeField] public IapProductKind ProductKind { get; set; } = IapProductKind.Consumable;
        [field: SerializeField] public string AndroidProductID { get; set; }
        [field: SerializeField] public string IOSProductID { get; set; }

        [field: SerializeField] public string Price { get; set; }
        [field: SerializeField] public string LocalizedPriceString { get; set; }
        [field: SerializeField] public decimal LocalizedPrice { get; set; }
        [field: SerializeField] public string CurrencyCode { get; set; } = "USD";
        [field: SerializeField] public bool IsConnectedToStore { get; set; }

        public bool IsRemoveAds => ProductKind == IapProductKind.RemoveAds;

        public IAPPackage(string productID, string price)
        {
            ProductID = productID;
            Price = $"${price}";
        }

        public string GetPrice()
        {
#if UNITY_EDITOR
            return Price;
#else
            return IsConnectedToStore ? LocalizedPriceString : Price;
#endif
        }

        public void SyncProductKindFromUnityType()
        {
            if (ProductKind == IapProductKind.RemoveAds)
                return;
            ProductKind = ProductType switch
            {
                ProductType.Subscription => IapProductKind.Subscription,
                ProductType.NonConsumable => IapProductKind.NonConsumable,
                _ => IapProductKind.Consumable
            };
        }
    }
}
#endif
