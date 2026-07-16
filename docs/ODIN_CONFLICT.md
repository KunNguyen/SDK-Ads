# Odin Inspector (Sirenix) — SDK ≥ 5.1 không còn dùng Odin

Từ **SDK 5.1**, JIS SDK **không dùng và không ship Odin Inspector** nữa:

- Package `com.jis.sdkads.odin` đã bị **xóa hẳn** khỏi SDK.
- `com.jis.sdkads.core` / `com.jis.sdkads.editor` không còn dependency tới Odin.
- Editor của SDK (`AdsManagerEditor`, `JisSDKAdsSettingsEditor`) dùng `UnityEditor.Editor` thuần.
- Runtime chưa bao giờ dùng Odin — không đổi gì về serialize, prefab, build.

Nhờ vậy SDK **không còn xung đột** với bản Odin Inspector riêng của từng project (Asset Store), và không còn vấn đề license khi phân phối DLL Sirenix.

## Migration từ SDK ≤ 5.0.x

Project cũ vẫn còn `com.jis.sdkads.odin` trong `Packages/manifest.json`. Nó không tự biến mất khi update SDK.

1. Update các package `com.jis.sdkads.*` lên ≥ 5.1 (Hub → **Fix com.jis.sdkads.\* revisions**)
2. Mở **JIS SDK → Hub** — nếu còn `com.jis.sdkads.odin`, Hub hiện cảnh báo kèm nút **Remove legacy com.jis.sdkads.odin** (gỡ khỏi manifest + flush PackageCache)
3. Package Manager → **Resolve** (hoặc restart Unity)

Gỡ tay (không cần Hub): xóa dòng `"com.jis.sdkads.odin"` trong `Packages/manifest.json`, xóa `Library/PackageCache/com.jis.sdkads.odin@*`, rồi Resolve.

## Nếu code của bạn (hoặc package `com.tw.*`) đang dùng Odin

Trước đây các code này compile được nhờ bản Odin mà SDK bundle. Sau khi gỡ:

- Cài **Odin Inspector chính chủ từ Asset Store** vào `Assets/Plugins/Sirenix` (cần license hợp lệ).
- Asmdef nào reference DLL Sirenix qua `precompiledReferences` vẫn hoạt động bình thường với bản Asset Store (Unity resolve theo tên DLL).

## Checklist sau migration

- [ ] Manifest không còn `com.jis.sdkads.odin`
- [ ] `Library/PackageCache` không còn `com.jis.sdkads.odin@*`
- [ ] Nếu project dùng Odin: chỉ một bản duy nhất tại `Assets/Plugins/Sirenix` (Asset Store)
- [ ] Hub không hiện cảnh báo Odin
- [ ] Compile sạch, inspector `AdsManager` / `JisSDKAdsSettings` hiển thị bình thường

## Liên quan

- [GAME_SETUP.md](GAME_SETUP.md)
- [MIGRATION_GUID_CONFLICT.md](MIGRATION_GUID_CONFLICT.md)
