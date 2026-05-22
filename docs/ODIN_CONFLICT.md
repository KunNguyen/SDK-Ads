# Xung đột Odin Inspector (Sirenix) — nhiều package / Assets

Unity **không** cho phép hai bản `Sirenix.OdinInspector.Attributes.dll` (và các DLL Odin khác) cùng tên assembly trong một project. Khi trùng, Unity thường **không load được assembly nào** → lỗi:

```text
error CS0246: The type or namespace name 'Sirenix' could not be found
```

(ví dụ `com.tw.utility` → `EditorColorGlobalConfig.cs`)

## Nguyên tắc

| Quy tắc | Chi tiết |
|--------|----------|
| **Một nguồn Odin** | Chỉ **một** trong: `com.jis.sdkads.core`, `Assets/Plugins/Sirenix`, hoặc **một** package UPM khác có nhúng Sirenix |
| **Không trộn** | Vừa Asset Store Odin trong `Assets` vừa Odin trong package → conflict |
| **PackageCache** | Không sửa trực tiếp `Library/PackageCache/...` — copy package vào `Packages/` hoặc đợi bản fix từ vendor |

## Cách tìm bản trùng

1. **JIS SDK → Hub** — cảnh báo liệt kê mọi đường dẫn `Sirenix.OdinInspector.Attributes.dll`
2. Hoặc trong project (PowerShell / bash):

```powershell
Get-ChildItem -Recurse -Filter "Sirenix.OdinInspector.Attributes.dll" `
  Assets, Packages, Library\PackageCache -ErrorAction SilentlyContinue |
  Select-Object FullName
```

## Các tình huống và cách xử lý

### A) Dùng JIS SDK (`com.jis.sdkads.core` có Odin) — **khuyến nghị**

1. Cập nhật **`com.jis.sdkads.core` ≥ 4.0.1** (sửa import Editor cho Attributes DLL).
2. **Xóa** `Assets/Plugins/Sirenix` nếu có.
3. Package **khác** (không phải JIS) cũng nhúng `Plugins/Sirenix`:
   - **Cách 1 (nhanh):** Fork/embed package đó vào `Packages/<tên-package>/` và **xóa** thư mục `Plugins/Sirenix` trong bản embed.
   - **Cách 2:** Yêu cầu vendor gỡ Odin khỏi package, phụ thuộc `com.jis.sdkads.core` hoặc Odin Asset Store chung.
   - **Cách 3:** Trong Unity, khi có hộp thoại **Plugin import conflict** → chọn bản từ `Packages/com.jis.sdkads.core/...`, **disable** bản từ package kia (chỉ ổn định nếu không re-import package).
4. Hub → **Flush PackageCache** (`com.jis.sdkads.*`) → Package Manager → **Resolve**.
5. Kiểm tra Hub: không còn “Multiple Odin copies” và compile sạch.

### B) Chỉ dùng Odin từ package khác (không dùng Odin trong JIS core)

Hiện JIS SDK **nhúng** Odin trong `core`. Nếu team bắt buộc dùng bản Odin của package X:

- Không thể giữ hai bản — phải **gỡ Odin khỏi một phía** (thường là fork `com.jis.sdkads.core` bỏ `Plugins/Sirenix`, hoặc dùng bản SDK không bundle Odin — liên hệ maintainer SDK).
- Mọi package (`com.tw.*`, `com.jis.sdkads.ads`, …) vẫn cần **cùng một** assembly `Sirenix.OdinInspector.Attributes`.

### C) Chỉ dùng Odin Asset Store trong `Assets`

1. Cài Odin vào `Assets/Plugins/Sirenix`.
2. **Gỡ** Odin khỏi mọi UPM package (core + package khác) — chỉ thực hiện được bằng fork hoặc bản SDK tùy chỉnh.
3. Không khuyến nghị khi đã dùng `com.jis.sdkads.core` chuẩn từ Git.

## `com.tw.*` không phải lỗi của JIS SDK

`com.tw.utility`, `com.tw.gui`, … **chỉ dùng** Odin; họ thường **không** ship DLL Sirenix. Lỗi `CS0246` nghĩa là **toàn project** không load được Odin vì conflict hoặc thiếu `com.jis.sdkads.core`.

## Checklist

- [ ] Chỉ còn **một** file `Sirenix.OdinInspector.Attributes.dll` (hoặc một bộ DLL được Unity resolve không conflict)
- [ ] Đã xóa `Assets/Plugins/Sirenix` nếu dùng `com.jis.sdkads.core`
- [ ] `com.jis.sdkads.core` ≥ **4.0.1** trong manifest
- [ ] Đã xử lý package khác có Odin (fork / vendor / disable duplicate)
- [ ] Hub không báo “Multiple Odin copies”
- [ ] `com.tw.utility` compile được

## Liên quan

- [MIGRATION_GUID_CONFLICT.md](MIGRATION_GUID_CONFLICT.md) — GUID + Odin Addressables module
- [GAME_SETUP.md](GAME_SETUP.md) — setup game + `com.tw.*`
