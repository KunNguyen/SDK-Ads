#if UNITY_IAP_ACTIVE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace JisSDKAds.IAP
{
    [System.Serializable]

    public class IAPPackage
    {
        [field: SerializeField] public string ProductID { get; set; }
        [field: SerializeField] public ProductType ProductType { get; set; }
        [field: SerializeField] public string AndroidProductID { get; set; }
        [field: SerializeField] public string IOSProductID { get; set; }

        [field: SerializeField] public string Price { get; set; }
        [field: SerializeField] public string LocalizedPriceString { get; set; }
        [field: SerializeField] public decimal LocalizedPrice { get; set; }
        [field: SerializeField] public string CurrencyCode { get; set; } = "USD";
        [field: SerializeField] public bool IsConnectedToStore { get; set; }

        public IAPPackage(string productID, string price)
        {
            ProductID = productID;
            Price = $"${price}";
        }

        public string GetPrice()
        {
#if UNITY_EDITOR
            return Price;
#endif
            var s = IsConnectedToStore ? LocalizedPriceString : Price;
            return s;
        }
    }
}
#endif
