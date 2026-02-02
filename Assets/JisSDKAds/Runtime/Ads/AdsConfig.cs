using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SDK
{
    [System.Serializable]
    public class AdsConfig
    {
        public AdsType adsType;
        public AdsMediationType adsMediationType = AdsMediationType.MAX;
        public bool isActive = true;
        private int adsReloadTime = 0;
        public int maxReloadTime = 3;
        private int currentAdsID = 0;
        public List<AdsMediationController> adsMediations = new List<AdsMediationController>();

        public void Init(AdsMediationController adsMediationController, UnityAction<ImpressionData> adRevenuePaidCallback = null)
        {
            if (adsMediationController != null)
            {
                adsMediations.Add(adsMediationController);
                adsMediationType = adsMediationController.GetAdsMediationType();
                adsMediationController.IsActive = isActive;
                adsMediationController.AdRevenuePaidCallback = adRevenuePaidCallback;
                isActive = true;
            }
            else
            {
                isActive = false;
            }
        }

        public void RefreshLoadAds()
        {
            adsReloadTime = 0;
        }

        public void MarkReloadFail()
        {
            adsReloadTime++;
            if (IsGoodToChangeAds())
            {
                adsReloadTime = 0;
                currentAdsID++;
                if (currentAdsID >= adsMediations.Count)
                {
                    currentAdsID = 0;
                }
            }
        }

        public bool IsGoodToChangeAds()
        {
            return adsReloadTime >= maxReloadTime;
        }

        public AdsMediationController GetAdsMediation()
        {
            return currentAdsID >= adsMediations.Count ? null : adsMediations[currentAdsID];
        }

        public AdsMediationController GetAdsMediation(AdsMediationType adsType)
        {
            foreach (AdsMediationController adsMediationController in adsMediations.Where(adsMediationController => adsMediationController.GetAdsMediationType() == adsType))
            {
                return adsMediationController;
            }

            return adsMediations[0];
        }
    }
}