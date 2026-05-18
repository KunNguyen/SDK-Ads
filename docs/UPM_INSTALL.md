# Cài JIS SDK vào project Unity khác (UPM)

Hướng dẫn thêm **SDK-Ads** vào game project qua **Unity Package Manager (UPM)** + **Git URL**. Repo là monorepo: mỗi module nằm trong `Packages/com.jis.sdkads.*`.

**Phiên bản hiện tại:** `4.0.0`  
**Unity:** 2022.3 LTS trở lên (khuyến nghị)

Sau khi cài package, làm tiếp: [GAME_SETUP.md](GAME_SETUP.md).  
Nâng cấp từ SDK cũ: [MIGRATION_V4.md](MIGRATION_V4.md).

---

## Có bắt buộc sửa `manifest.json` tay không?

**Không.** Sửa `Packages/manifest.json` chỉ là một cách; Unity ghi cùng nội dung khi bạn cài qua UI.

| Cách | `com.jis.sdkads.*` | Ghi chú |
|------|-------------------|---------|
| **Package Manager → Add package from git URL…** | ✅ Có | Dán Git URL Hub — **khuyến nghị** nếu không muốn mở JSON |
| **Sửa `manifest.json` tay** | ✅ Có | Tương đương UI; tiện cho copy/paste doc hoặc CI |
| **JIS SDK → Hub → Import …** | ✅ Có (sau khi đã có Hub) | Hub **tự sửa** manifest + defines — không cần thêm từng package JIS bằng tay |
| **OpenUPM** (`openupm add …`) | ❌ Hiện **chưa** publish | Package JIS chưa có trên [openupm.com](https://openupm.com); chỉ dùng OpenUPM cho dependency khác (vd. `com.google.ads.mobile` khi Hub Import Ads) |

**Luồng thực tế gọn nhất:**

1. Package Manager → **+** → **Add package from git URL…** → URL Hub (một lần).
2. **JIS SDK → Hub** → Import Firebase, Ads, … (Hub lo phần còn lại).

Bạn **không** cần mở `manifest.json` trừ khi muốn review diff hoặc script hóa build.

### Package Manager (Git URL) — từng bước

1. `Window → Package Manager`
2. Góc trên trái: chọn **Packages: In Project** hoặc **Unity Registry** (không quan trọng)
3. Nút **+** → **Add package from git URL…**
4. Dán:

   ```text
   https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#4.0.0
   ```

5. **Add** → đợi clone/resolve → menu **JIS SDK → Hub** xuất hiện

Unity 2022.3+ hỗ trợ Git URL có `?path=` và `#tag`. Repo private: Git/SSH phải hoạt động trên máy (giống khi sửa manifest).

### OpenUPM

- **JIS SDK (`com.jis.sdkads.*`):** chưa đăng lên registry OpenUPM → **không** cài bằng `openupm add com.jis.sdkads.hub` cho đến khi team publish (xem [OpenUPM — Publish](https://github.com/openupm/openupm#publish-a-package)).
- **AdMob UPM:** Hub có thể thêm `com.google.ads.mobile` qua scoped registry OpenUPM khi Import **Ads** — đó là package Google, không phải JIS.

Nếu sau này publish lên OpenUPM, game có thể thêm scoped registry + cài bằng CLI/UI OpenUPM; khi đó sẽ cập nhật doc với lệnh `openupm add` cụ thể.

---

## Yêu cầu trước khi cài

| Yêu cầu | Ghi chú |
|---------|---------|
| Git | Unity clone package qua Git — cần [Git](https://git-scm.com/) trên máy |
| Quyền đọc repo | Public GitHub hoặc SSH/token nếu repo private |
| Firebase (game) | **Không** nằm trong SDK — cài [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) (Analytics + Remote Config) trong project game |
| Ads mediation | MAX và/hoặc AdMob — Hub thêm registry + package khi Import **Ads** |

---

## Cách 1 — Khuyến nghị: Hub (một Git URL, import từng module)

### Bước 1: Thêm package Hub

Mở `YourGame/Packages/manifest.json`, thêm vào `dependencies`:

```json
"com.jis.sdkads.hub": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#4.0.0"
```

Thay URL bằng repo của bạn nếu fork/private:

```text
https://github.com/<ORG>/<REPO>.git?path=Packages/com.jis.sdkads.hub#4.0.0
```

**Định dạng Git UPM:**

```text
<git-url>?path=Packages/<tên-thư-mục-package>#<tag-hoặc-branch>
```

- `#4.0.0` — tag/release (ổn định, khuyến nghị production)
- `#main` — branch (dev, có thể đổi bất cứ lúc nào)

Lưu file → Unity resolve package (có thể mất vài phút lần đầu).

**Hoặc qua UI:** `Window → Package Manager → + → Add package from git URL…` → dán cùng URL trên.

### Bước 2: Mở Hub

Menu: **JIS SDK → Hub**

- Tắt **Use embedded packages (SDK-Ads dev repo)** — chỉ bật khi đang làm việc trong repo SDK-Ads.
- **Git UPM base URL:** ví dụ `https://github.com/KunNguyen/SDK-Ads.git` → **Save Git URL**  
  Hub dùng URL này + `#4.0.0` khi bấm Import.

### Bước 3: Import module (thứ tự gợi ý)

| Thứ tự | Nút Hub | Package JIS được thêm | Ghi chú |
|--------|---------|------------------------|---------|
| 1 | **Firebase** | hub, core, common, firebase | Cài Firebase Google trước/sau, bắt buộc có trong game |
| 2 | **Ads** | providers.max, providers.admob, ads | Thêm AppLovin MAX UPM; AdMob UPM nếu chưa có |
| 3 | (tuỳ chọn) IAP, App Review, AppsFlyer, … | từng package tương ứng | Hub thêm define + dependency Unity |

Sau **Import Ads**, Hub tự:

- Thêm **scoped registries** (AppLovin, Google, OpenUPM) nếu chưa có
- Thêm `com.applovin.mediation.ads`, `com.google.ads.mobile` (nếu chưa có)
- Tạo `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset` + stub `AndroidSDKSetup` / `IOSSDKSetup`
- Bật scripting defines: `UNITY_AD_MAX`, `UNITY_AD_ADMOB`, …

### Bước 4: Kiểm tra

- **Package Manager:** thấy các package `com.jis.sdkads.*`
- **Console:** không lỗi compile
- Tiếp theo: [GAME_SETUP.md](GAME_SETUP.md) (scene, settings, code)

---

## Cách 2 — Thêm từng package thủ công (không dùng Hub)

Chỉ dùng khi CI/script hoặc bạn muốn kiểm soát từng dòng `manifest.json`.

**Base URL** (ví dụ):

```text
https://github.com/KunNguyen/SDK-Ads.git
```

**Suffix chung:**

```text
?path=Packages/<folder>#4.0.0
```

### Tối thiểu để chạy Ads + Firebase

```json
{
  "dependencies": {
    "com.jis.sdkads.hub": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#4.0.0",
    "com.jis.sdkads.core": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.core#4.0.0",
    "com.jis.sdkads.common": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.common#4.0.0",
    "com.jis.sdkads.firebase": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.firebase#4.0.0",
    "com.jis.sdkads.providers.max": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.providers.max#4.0.0",
    "com.jis.sdkads.providers.admob": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.providers.admob#4.0.0",
    "com.jis.sdkads.ads": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.ads#4.0.0",
    "com.applovin.mediation.ads": "8.6.3",
    "com.google.ads.mobile": "9.4.0"
  },
  "scopedRegistries": [
    {
      "name": "AppLovin MAX Unity",
      "url": "https://unity.packages.applovin.com/",
      "scopes": [
        "com.applovin.mediation.ads",
        "com.applovin.mediation.adapters",
        "com.applovin.mediation.dsp"
      ]
    },
    {
      "name": "Game Package Registry by Google",
      "url": "https://unityregistry-pa.googleapis.com",
      "scopes": ["com.google"]
    },
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.google.ads.mobile"]
    }
  ]
}
```

**Scripting defines** (Player Settings → Other Settings → Scripting Define Symbols), ít nhất cho Ads:

```text
UNITY_AD_MAX;UNITY_AD_ADMOB
```

| Module | Package id | Define (nếu có) |
|--------|------------|-----------------|
| IAP | `com.jis.sdkads.iap` | `UNITY_IAP_ACTIVE` |
| App Review | `com.jis.sdkads.appreview` | `GOOGLE_REVIEW` |
| AppsFlyer | `com.jis.sdkads.analytics.appsflyer` | `UNITY_APPSFLYER` |
| Editor tools | `com.jis.sdkads.editor` | — |
| Samples | `com.jis.sdkads.samples` | — (chỉ README mẫu) |

Dependency giữa package JIS đã khai báo trong từng `package.json` — Unity resolve theo thứ tự khi bạn thêm đủ nhánh cần dùng.

---

## Repo private / SSH

**SSH** (khuyến nghị cho private):

```json
"com.jis.sdkads.hub": "git@github.com:<ORG>/<REPO>.git?path=Packages/com.jis.sdkads.hub#4.0.0"
```

Máy build/CI cần SSH key hoặc credential helper.

**HTTPS + token** (ít dùng trong manifest vì lộ token):

```text
https://<TOKEN>@github.com/<ORG>/<REPO>.git?path=Packages/...
```

Ưu tiên SSH hoặc [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager).

---

## Test local (không qua Git)

Khi SDK và game cùng máy, có thể trỏ **đường dẫn tuyệt đối** (chỉ dev):

```json
"com.jis.sdkads.hub": "file:F:/Workspaces/SDK-Ads/Packages/com.jis.sdkads.hub"
```

Hoặc copy/symlink folder `Packages/com.jis.sdkads.*` vào `YourGame/Packages/` — Unity coi là embedded package (giống repo SDK-Ads dev).

Trong Hub: bật **Use embedded packages** khi đang mở project SDK-Ads.

---

## Nâng / hạ phiên bản SDK

1. Đổi tag trong mọi dòng Git UPM: `#4.0.0` → `#4.1.0`
2. Hoặc trong Hub: cập nhật **Git UPM base URL** + re-import module (Hub ghi đè version trong manifest)
3. Đọc [MIGRATION_V4.md](MIGRATION_V4.md) nếu đổi major
4. `Assets → Reimport All` hoặc xóa `Library/PackageCache` nếu Unity cache lỗi

**Lưu ý:** Tất cả package `com.jis.sdkads.*` trong một lần release nên cùng tag (`#4.0.0`) để tránh lệch API giữa `ads` và `core`.

---

## Samples (tuỳ chọn)

```json
"com.jis.sdkads.samples": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.samples#4.0.0"
```

Package Manager → **JIS SDK - Samples** → **Samples** → *Minimal integration*.

---

## Xử lý lỗi thường gặp

| Triệu chứng | Cách xử lý |
|-------------|------------|
| `Cannot clone` / authentication | Cài Git; kiểm tra SSH/`gh auth`; quyền repo |
| Package không resolve | URL đúng `?path=Packages/com.jis.sdkads.xxx`; tag `#4.0.0` tồn tại trên remote |
| Lỗi AppLovin / Google Ads | Thêm `scopedRegistries` như mục Cách 2 hoặc Import Ads qua Hub |
| Lỗi Firebase types | Cài Firebase Unity từ Google vào **game project** |
| Hub không có menu | Chỉ có sau khi `com.jis.sdkads.hub` compile xong |
| `file:` không hoạt động trên máy khác | Dùng Git URL thay vì `file:` khi chia sẻ project |

---

## Tóm tắt nhanh

1. Thêm `com.jis.sdkads.hub` vào `Packages/manifest.json` (Git URL + `#4.0.0`)
2. **JIS SDK → Hub** → tắt embedded → Save Git URL
3. Import **Firebase** → cài Firebase Google
4. Import **Ads** → chỉnh settings + scene ([GAME_SETUP.md](GAME_SETUP.md))
5. Code: `JisAds.Instance` ([MIGRATION_V4.md](MIGRATION_V4.md))
