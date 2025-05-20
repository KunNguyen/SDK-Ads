using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine;

namespace SDK
{
     public partial class AdsManager
     {
          private AdsConfig MRecAdsConfig => GetAdsConfig(AdsType.MREC);
          [field: SerializeField] public MRecAdManager MRecAdManager { get; set; }
          private void SetupMRecAds()
          {
               DebugAds.Log("Setup MRec");
               MRecAdManager.Setup(MRecAdsConfig, SDKSetup, GetSelectedMediation(AdsType.MREC));
               OnRemoveAdsEvent.AddListener(MRecAdManager.OnRemoveAd);
               MRecAdManager.IsRemoveAds = () => IsRemoveAds;
               MRecAdManager.IsCheatAds = () => IsCheatAds;
          }
          private void InitMRecAds(AdsMediationType adsMediationType)
          {
               DebugAds.Log("Init MRec");
               MRecAdManager.Init(adsMediationType);
          }
          public void ShowMRecAds()
          {
               MRecAdManager.Show();
               HideBannerAds();
          }
          public void HideMRecAds()
          {
               DebugAds.Log("Call Hide MRec Ads");
               MRecAdManager.Hide();
          }
          public bool IsMRecShowing()
          {
               return MRecAdManager.IsShowingAd;
          }
          public bool CanShowMRecAd()
          {
               return MRecAdManager.IsAdReady();
          }
     }
}