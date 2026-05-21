# Xử lý lỗi GUID conflict (ABIMaxSDKAds + UPM)

## Triệu chứng

```
GUID [xxxxxxxx] for asset 'Packages/com.jis.sdkads.ads/...' conflicts with:
  'Assets/ABIMaxSDKAds/Scripts/...' (current owner)
We can't assign a new GUID because the asset is in an immutable folder. The asset will be ignored.
```

Sau đó hàng loạt lỗi `JisSDKAds.Common does not exist`, `SDKSetup could not be found`, v.v.

## Nguyên nhân

Project game vẫn còn **bản SDK cũ** trong `Assets/ABIMaxSDKAds/` (và có thể `Assets/Plugins/Sirenix/`).  
Các file đó **dùng chung GUID** với package UPM `com.jis.sdkads.*` → Unity chỉ nhận bản trong `Assets/`, **bỏ qua toàn bộ code trong Package Cache** → SDK “mất” hoàn toàn.

Đây **không phải** lỗi Firebase hay IAP riêng lẻ — phải xóa legacy trước.

---

## Cách sửa (bắt buộc trên project game)

### Bước 1 — Backup & đóng Unity

Commit hoặc backup project trước khi xóa folder lớn.

### Bước 2 — Xóa SDK cũ trong Assets

Trong project game (ví dụ `JIS-CardShop`), **xóa** (hoặc move ra ngoài project):

| Xóa | Lý do |
|-----|--------|
| `Assets/ABIMaxSDKAds/` | Trùng `com.jis.sdkads.ads`, `common`, `firebase`, `iap`, … |
| `Assets/JisSDKAds/` | Monolith cũ (nếu còn) |
| `Assets/Plugins/Sirenix/` | Trùng Odin trong `com.jis.sdkads.core` (nếu dùng package core) |

**Giữ lại** (nếu vẫn cần):

- `Assets/Firebase/` — Firebase Unity SDK từ Google
- `Assets/GoogleSignIn/` — nếu dùng Google Sign-In
- `Assets/JisSDKAds/Settings/` — asset settings (đường dẫn mới v4)
- Mediation: AppsFlyer, MAX, AdMob trong Assets (không trùng package JIS)

### Bước 3 — Cập nhật package JIS từ Git

Trong `Packages/manifest.json` trỏ tag/commit **mới nhất** (sau khi repo SDK đã regenerate GUID).

Xóa cache cũ (Unity đóng):

- `Library/PackageCache/com.jis.sdkads.*`

Mở lại Unity → để reimport.

### Bước 4 — Hub import lại

**JIS SDK → Hub** → Firebase, Ads, IAP, … (theo nhu cầu).

**JIS SDK → Ads → Create/Open Settings Asset**  
**Apply Settings to Scene**

### Bước 5 — Kiểm tra Console

Không còn dòng `GUID ... conflicts with ... ABIMaxSDKAds`.

---

## Prefab / Scene bị Missing Script

Sau khi xóa `ABIMaxSDKAds`, prefab/scene có thể mất reference script cũ.

1. Mở prefab `AdsManager` / `JisSDK_Manager`
2. **JIS SDK → Ads → Scene → Add Manager Prefab** (tạo lại)
3. Gán lại `JisSDKAdsSettings`, `IAPPackageConfigs`, …

---

## Checklist nhanh

- [ ] Đã xóa `Assets/ABIMaxSDKAds`
- [ ] Đã xóa `Assets/Plugins/Sirenix` (nếu conflict Sirenix)
- [ ] Package `com.jis.sdkads.*` bản mới (GUID mới)
- [ ] Không còn warning GUID conflict
- [ ] Compile OK (`JisSDKAds.Ads`, `JisSDKAds.Common`, …)

---

## SDK repo (maintainer)

Chạy khi copy code từ legacy sang package:

```powershell
powershell -File scripts/RegeneratePackageGuids.ps1
```

Commit `.meta` mới — **không** reuse GUID từ `Assets/ABIMaxSDKAds`.
