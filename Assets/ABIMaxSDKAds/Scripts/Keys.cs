using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABI {
     public class Keys {
        
          internal static readonly string key_remote_aoa_active = "show_open_ads";
          internal static readonly string key_remote_resume_ads_type = "show_open_ads_resume";
          internal static readonly string key_remote_aoa_show_first_time_active = "show_open_ads_first_open";

          internal static readonly string key_remote_ads_resume_ads_active = "ads_resume_active";
          internal static readonly string key_remote_ads_resume_capping_time = "ads_resume_capping_time";
          internal static readonly string key_remote_ads_resume_pause_time = "ads_resume_pause_time";
          internal static readonly string key_remote_ads_resume_ads_type = "ads_resume_type";
        
          internal static readonly string key_remote_interstitial_level = "level_show_inter";
          internal static readonly string key_remote_interstitial_capping_time = "ads_interval";
        
          internal static readonly string key_remote_inter_reward_interspersed = "inter_reward_interspersed";
          internal static readonly string key_remote_inter_reward_interspersed_time = "inter_reward_interspersed_time";
          internal static readonly string key_remote_free_ads = "time_free_ads";
          internal static readonly string key_remote_mrec_active = "show_mrec_admob";
     } 
}