# Odin Inspector (Sirenix) — một nguồn duy nhất

Unity **không** cho phép hai bản `Sirenix.OdinInspector.Attributes.dll` (và các DLL Odin khác) cùng tên assembly trong một project. Khi trùng, Unity thường **không load được assembly nào** → lỗi:

```text
error CS0246: The type or namespace name 'Sirenix' could not be found
```

## Kiến trúc JIS SDK ≥ 5.0.0

| Package | Odin |
|---------|------|
| **com.jis.sdkads.odin** | Chứa toàn bộ `Plugins/Sirenix` (DLL + editor) — **nguồn duy nhất** |
| **com.jis.sdkads.core** | Không chứa Sirenix; phụ thuộc `com.jis.sdkads.odin` (UPM tự cài) |
| **com.jis.sdkads.ads** | **Runtime không dùng Odin** (chỉ `SerializeField` / Unity Inspector) |
| **com.jis.sdkads.editor** | Odin cho `AdsManagerEditor` (OdinEditor) |
| **com.tw.\*** (bên ngoài) | Chỉ dùng attribute; **không** ship DLL — cần reference tới `com.jis.sdkads.odin` |

## Nguyên tắc

| Quy tắc | Chi tiết |
|--------|----------|
| **Một nguồn Odin** | `com.jis.sdkads.odin` **hoặc** Asset Store `Assets/Plugins/Sirenix` — không trộn |
| **Không nhúng Sirenix** trong package UPM khác | Fork vendor và xóa `Plugins/Sirenix` |
| **Không copy config vào Assets** | Không cần `Assets/Packages/com.jis.sdkads.core/Plugins/Sirenix` |
| **Hub** | Tự quét duplicate + cố gắng disable plugin import thừa |

## Cách tìm bản trùng

1. **JIS SDK → Hub** — cảnh báo liệt kê mọi `Sirenix.OdinInspector.Attributes.dll`
2. PowerShell:

```powershell
Get-ChildItem -Recurse -Filter "Sirenix.OdinInspector.Attributes.dll" `
  Assets, Packages, Library\PackageCache -ErrorAction SilentlyContinue |
  Select-Object FullName
```

## Các tình huống

### A) Project mới / cập nhật lên SDK 5.x (khuyến nghị)

1. Hub → **Fix com.jis.sdkads.\* revisions** (≥ 5.0.0)
2. Import **Firebase** hoặc **Editor** module (cài `com.jis.sdkads.odin` + `core`)
3. Xóa `Assets/Plugins/Sirenix` nếu có
4. Package khác có `Plugins/Sirenix` → fork và xóa thư mục đó
5. Hub → Flush PackageCache → Resolve
6. Kiểm tra Hub: một DLL, compile sạch

### B) Đã có Odin Asset Store trong Assets

1. Chọn **một**: giữ Asset Store **hoặc** `com.jis.sdkads.odin`
2. Nếu giữ Asset Store: gỡ `com.jis.sdkads.odin` khỏi manifest (không khuyến nghị với JIS editor)
3. Nếu giữ JIS odin: xóa `Assets/Plugins/Sirenix`

### C) `com.tw.*` lỗi CS0246 sau khi cài SDK

TW không ship DLL. Cần asmdef reference tới Odin trong `com.jis.sdkads.odin`:

- Sửa repo TW: thêm `precompiledReferences` + `overrideReferences: true` (khuyến nghị)
- Hoặc script game `TwOdinAsmdefPatcher` (workaround tạm)

## Nâng cấp từ 4.x → 5.0

- Odin chuyển từ `core` → **`com.jis.sdkads.odin`**
- `ads` runtime **không còn** attribute Odin — không cần patch asmdef ads
- Xóa folder tùy biến `Assets/Packages/com.jis.sdkads.core` chỉ chứa config Sirenix (nếu có)
- Manifest: thêm/để UPM resolve `com.jis.sdkads.odin` qua dependency `core`

## Checklist

- [ ] Chỉ một `Sirenix.OdinInspector.Attributes.dll` active
- [ ] `com.jis.sdkads.odin` ≥ 5.0.0 (trực tiếp hoặc qua `core`)
- [ ] Không `Assets/Plugins/Sirenix` khi dùng JIS odin
- [ ] Package vendor khác không bundle Sirenix
- [ ] Hub không báo "Multiple Odin copies"

## Liên quan

- [GAME_SETUP.md](GAME_SETUP.md)
- [MIGRATION_GUID_CONFLICT.md](MIGRATION_GUID_CONFLICT.md)
