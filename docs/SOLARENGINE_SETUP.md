# SolarEngine + JIS SDK

JIS bridge: `com.jis.sdkads.analytics.solarengine` — **không** chứa SolarEngine C# SDK.

## Yêu cầu (cả hai)

| # | Thành phần | Ghi chú |
|---|------------|---------|
| 1 | **SolarEngine Unity SDK (C#)** | Thư mục `Assets/SolarEngineSDK/` + file `SolarEngineSDK.asmdef` |
| 2 | **JIS module** | `com.jis.sdkads.analytics.solarengine` qua Hub |
| 3 | **Define** | `UNITY_SOLAR_ENGINE` (Hub tự thêm khi import module SolarEngine) |

Chỉ import `Assets/SolarEngineNet` (native) **không đủ** — cần cả **Scripts** (`Analytics`, `SEConfig`, …).

## Kiểm tra nhanh

```powershell
Test-Path "Assets\SolarEngineSDK\SolarEngineSDK.asmdef"
```

Phải trả về `True`.

## Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|-----|-------------|------------|
| `Analytics` does not exist in namespace `SolarEngine` | Thiếu asmdef reference tới `SolarEngineSDK` hoặc chưa có C# SDK | Cài đủ SDK; cập nhật JIS ≥ 5.0; Resolve packages |
| `SolarEngine` namespace not found | Không có `Assets/SolarEngineSDK` | Import bản Unity SDK đầy đủ từ SolarEngine |
| Assembly `SolarEngineSDK` not found | Sai tên asmdef hoặc SDK nằm ngoài `Assets` | Giữ asmdef tên `SolarEngineSDK` (mặc định vendor) |

## Setup scene

1. Tạo GameObject với `SolarEngineManager` (hoặc prefab từ team).
2. Gán **App Key** SolarEngine.
3. Ads impression / IAP được bridge tự động khi `AdsManager` + IAP active.

## Namespace JIS

Code bridge dùng `JisSDKAds.Analytics.SolarEngineIntegration` — gọi vendor API qua alias `SE = global::SolarEngine` (tránh `Analytics` bị hiểu nhầm thành namespace `JisSDKAds.Analytics`).
