using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JisSDKAds.Common {
     public static class Keys {
          public const string key_local_remove_ads = "key_local_remove_ads";

          public static readonly string key_remote_aoa_active = "show_open_ads";
          public static readonly string key_remote_aoa_show_first_time_active = "show_open_ads_first_open";

          public static readonly string key_remote_ads_resume_ads_active = "ads_resume_active";
          public static readonly string key_remote_ads_resume_capping_time = "ads_resume_capping_time";
          public static readonly string key_remote_ads_resume_pause_time = "ads_resume_pause_time";
          public static readonly string key_remote_ads_resume_ads_type = "ads_resume_type";

          public static readonly string key_remote_interstitial_level = "level_show_inter";
          public static readonly string key_remote_interstitial_capping_time = "ads_interval";
          
          // Interstitial capping v2:
          // - type1: block interstitial until X seconds after app open
          // - type2: minimum seconds between successful interstitial shows (also reset when rewarded watched)
          public static readonly string key_remote_interstitial_capping_from_app_open_seconds = "inter_capping_from_app_open_seconds";
          public static readonly string key_remote_interstitial_capping_between_shows_seconds = "inter_capping_between_shows_seconds";

          public static readonly string key_remote_inter_reward_interspersed = "inter_reward_interspersed";
          public static readonly string key_remote_inter_reward_interspersed_time = "inter_reward_interspersed_time";
          public static readonly string key_remote_free_ads = "time_free_ads";
          public static readonly string key_remote_banner_auto_refresh = "banner_auto_refresh";
          public static readonly string key_remote_banner_auto_refresh_time = "banner_auto_refresh_time";

          // Interstitial / Rewarded inventory mode: "single" (default) | "tiered"
          public static readonly string key_remote_interstitial_inventory_mode = "interstitial_inventory_mode";
          public static readonly string key_remote_rewarded_inventory_mode = "rewarded_inventory_mode";

          // Sequential tier unit IDs (Firebase Remote Config)
          public static readonly string key_remote_inter_premium_id = "inter_premium_id";
          public static readonly string key_remote_inter_high_id = "inter_high_id";
          public static readonly string key_remote_inter_mid_id = "inter_mid_id";
          public static readonly string key_remote_inter_low_id = "inter_low_id";
          public static readonly string key_remote_inter_fill_id = "inter_fill_id";

          public static readonly string key_remote_reward_premium_id = "reward_premium_id";
          public static readonly string key_remote_reward_high_id = "reward_high_id";
          public static readonly string key_remote_reward_mid_id = "reward_mid_id";
          public static readonly string key_remote_reward_low_id = "reward_low_id";
          public static readonly string key_remote_reward_fill_id = "reward_fill_id";
     } 
}