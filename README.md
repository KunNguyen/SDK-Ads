# JisSDKAds

SDK quảng cáo cho Unity (Unity 2021.3+, Unity 6), hỗ trợ AppLovin MAX, Google AdMob, IronSource.

---

## Cài đặt qua UPM (Unity Package Manager)

### Cách 1: Git URL (khuyến nghị)

1. Mở **Window** → **Package Manager**
2. Nhấn **+** → **Add package from git URL**
3. Nhập URL (thay `main` bằng branch nếu cần):

```
https://github.com/KunNguyen/SDK-Ads.git?path=/Assets/JisSDKAds#main
```

4. Nhấn **Add**

### Cách 2: Thêm vào manifest.json

Mở `Packages/manifest.json` và thêm vào `dependencies`:

```json
{
  "dependencies": {
    "com.jis.sdkads": "https://github.com/KunNguyen/SDK-Ads.git?path=/Assets/JisSDKAds#main"
  }
}
```

- `?path=/Assets/JisSDKAds` – chỉ định thư mục chứa `package.json`
- `#main` – branch (có thể đổi thành `#v3.0.0` cho tag cụ thể)

### Cách 3: Local path (phát triển)

Khi clone repo vào cùng thư mục với project:

```json
{
  "dependencies": {
    "com.jis.sdkads": "file:../SDK-Ads/Assets/JisSDKAds"
  }
}
```

> **Lưu ý:** Đường dẫn `file:` phải tương đối từ thư mục gốc project (chứa `Packages/`).

### ⚠️ Bắt buộc: Cài đặt Dependencies trước

JisSDKAds **cần** các package sau để compile. Cài đặt **trước** hoặc **cùng lúc** với JisSDKAds:

| Package | Cách cài |
|---------|----------|
| **Unity Purchasing** | Có sẵn trong Unity Registry |
| **Firebase** (App, Analytics, RemoteConfig) | [Firebase Unity Setup](https://firebase.google.com/docs/unity/setup) – import `.unitypackage` hoặc `.tgz` |
| **Google Mobile Ads** | [AdMob Quick Start](https://developers.google.com/admob/unity/quick-start) – import qua UPM hoặc `.unitypackage` |
| **AppLovin MAX** (nếu dùng MAX) | [MAX Integration](https://dash.applovin.com/documentation/mediation/unity/getting-started/integration) |

**Nếu thiếu dependencies**, bạn sẽ gặp lỗi như:
- `The type or namespace name 'GoogleMobileAds' could not be found`
- `The type or namespace name 'Firebase' could not be found`
- `The type or namespace name 'Purchasing' does not exist`

**Giải pháp:** Cài đủ Firebase, Google AdMob, Unity Purchasing theo hướng dẫn trên.

**Mẫu `manifest.json`** – thêm vào `dependencies` và `scopedRegistries` (nếu chưa có):

```json
{
  "dependencies": {
    "com.jis.sdkads": "https://github.com/JustIdleGames/SDK-Ads.git?path=/Assets/JisSDKAds#main",
    "com.unity.purchasing": "5.0.0"
  },
  "scopedRegistries": [
    {
      "name": "Game Package Registry by Google",
      "url": "https://unityregistry-pa.googleapis.com",
      "scopes": ["com.google"]
    },
    {
      "name": "AppLovin MAX Unity",
      "url": "https://unity.packages.applovin.com/",
      "scopes": ["com.applovin.mediation.ads", "com.applovin.mediation.adapters"]
    }
  ]
}
```

Sau đó cài **Firebase** và **Google AdMob** qua Package Manager (từ registry Google) hoặc import `.unitypackage` / `.tgz` theo tài liệu chính thức.

---

## Yêu cầu & Dependencies

| Package | Version | Link |
|---------|---------|------|
| Unity | 2021.3+ | - |
| Appsflyer | 6.15.3 | [GitHub](https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/tree/master) |
| Appsflyer ad revenue generic connector | 6.14.3 | [GitHub](https://github.com/AppsFlyerSDK/appsflyer-unity-adrevenue-generic-connector) |
| AppLovin MAX | 8.0.1 | [Docs](https://dash.applovin.com/documentation/mediation/unity/getting-started/integration) |
| Google AdMob | 9.4.0 | [Quick Start](https://developers.google.com/admob/unity/quick-start?hl=vi) |
| Firebase SDK | 12.4.1 | [Setup](https://firebase.google.com/docs/unity/setup) (Analytics, RemoteConfig) |
| Google Resolver | 1.2.179 | - |
| Google In-app review | - | [Unity Guide](https://developer.android.com/guide/playcore/in-app-review/unity) |

---

## Hướng dẫn tích hợp (Integration)

### Bước 1: Tạo SDKSetup Container

1. Menu → **SDK Setup** → **Create or Open SDKSetup Container**
2. Tự động tạo:
   - `Assets/JisSDKConfigs/AdsManagerSDKSetupContainer.asset`
   - `Assets/JisSDKConfigs/Platform/AndroidSDKAdsSetup.asset`
   - `Assets/JisSDKConfigs/Platform/IOSSDKAdsSetup.asset`

### Bước 2: Cấu hình Ads theo platform

1. Mở **AdsManagerSDKSetupContainer** (Inspector)
2. **Android** – gán `AndroidSDKAdsSetup` hoặc tạo mới: `Create` → **Tools** → **SDK Ads Setup**
3. **iOS** – gán `IOSSDKAdsSetup` hoặc tạo mới tương tự
4. Trong mỗi SDKSetup:
   - Chọn **Ads Mediation Type** (MAX, AdMob, IronSource)
   - Chọn mediation cho từng loại quảng cáo: Interstitial, Rewarded, Banner, Collapsible Banner, MRec, App Open
   - Nhập Ad Unit IDs tương ứng với SDK đã chọn

### Bước 3: Thêm AdsManager vào scene

1. Kéo prefab **AdsManager** vào scene (ví dụ: `Assets/JisSDKAds/Prefabs/AdsManager.prefab`)
2. Hoặc tạo GameObject mới và thêm component `AdsManager`

### Bước 4: Apply Setup

1. Đảm bảo AdsManager đã có trong scene
2. Chọn **AdsManagerSDKSetupContainer** → nhấn **Setup**
3. Hệ thống sẽ:
   - Gán `AndroidSdkSetup` và `IOSSdkSetup` vào AdsManager
   - Apply config theo **Build Target hiện tại** (Android/iOS)
   - Cập nhật Scripting Define Symbols

### Bước 5: Auto Apply (tùy chọn)

Menu **SDK Setup** → **AdsManager** → **Auto Apply**:

| Tùy chọn | Mô tả |
|----------|-------|
| Toggle On Platform Switch | Tự apply khi chuyển Build Target (Android ↔ iOS) |
| Toggle On Play | Tự apply trước khi vào Play mode |
| Toggle On Build | Tự apply trước khi Build |

**Apply thủ công:** **SDK Setup** → **AdsManager** → **Auto Apply** → **Apply Now (Active BuildTarget)**

---

## Scripting Define Symbols

SDK tự động thêm/sửa các symbol sau khi Setup:

| Symbol | Khi nào |
|--------|---------|
| `UNITY_AD_MAX` | Chọn Ads Mediation = MAX |
| `UNITY_AD_ADMOB` | Chọn Ads Mediation = AdMob |
| `UNITY_AD_IRONSOURCE` | Chọn Ads Mediation = IronSource |
| `UNITY_APPSFLYER` | Bật AppsFlyer trong SDKSetup |

---

## Các loại quảng cáo được hỗ trợ

- **Interstitial** – Quảng cáo toàn màn hình
- **Rewarded** – Quảng cáo thưởng
- **Banner** – Banner
- **Collapsible Banner** – Banner thu gọn
- **MRec** – Quảng cáo 300x250
- **App Open** – Quảng cáo khi mở app

---

## Build Note

- Bỏ Auto Graphics API, bỏ Vulkan (Project Settings → Other Settings)
- Đánh dấu cả x86 và x86-64 trong Target Architectures (Project Settings → Other Settings)
- Import cả FirebaseMessaging

---

## Troubleshooting

| Lỗi | Giải pháp |
|-----|-----------|
| "Please add Manager Prefab to scene" | Thêm AdsManager prefab vào scene trước khi Setup |
| "Không tìm thấy AdsManagerSDKSetupContainer" | Chạy **SDK Setup** → **Create or Open SDKSetup Container** |
| "Container chưa gán SDKSetup cho {platform}" | Gán Android/iOS setup trong AdsManagerSDKSetupContainer |
| Missing Script trên AdsManager | Đảm bảo đã build đúng với Define Symbols (UNITY_AD_MAX, UNITY_AD_ADMOB, v.v.) |

