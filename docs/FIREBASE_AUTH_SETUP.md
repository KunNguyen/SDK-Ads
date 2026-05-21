# Firebase Auth — lỗi compile thường gặp

## Triệu chứng

```
error CS0234: The type or namespace name 'Auth' does not exist in the namespace 'Firebase'
error CS0246: The type or namespace name 'FirebaseUser' could not be found
error CS0246: The type or namespace name 'Google' could not be found
```

## Nguyên nhân

1. **`FIREBASE_AUTH` đã bật** (trong `SDKSetup` → Apply Settings) nhưng project **chưa cài** module **Firebase Authentication** (Unity package từ Google) → thiếu `Firebase.Auth.dll`.

2. (Bản SDK cũ) **`GOOGLE_SIGNIN` define** + thiếu Google Sign-In plugin. **Bản mới** dùng reflection — **không cần** define `GOOGLE_SIGNIN`; chỉ cần plugin khi gọi `SignInWithGoogleAsync`.

SDK v4 tách Auth sang assembly `JisSDKAds.Firebase.Auth` (chỉ compile khi có `FIREBASE_AUTH` **và** `Firebase.Auth.dll` trong project).

## Cách xử lý

### A — Dùng Firebase Auth (khuyến nghị nếu cần login)

1. [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) → cài ít nhất:
   - Firebase App
   - **Firebase Authentication**
2. Trong `SDKSetup` (Android/iOS): bật **Firebase Auth**.
3. **JIS SDK → Ads → Apply Settings to Scene** (thêm define `FIREBASE_AUTH`).
4. Kiểm tra có file: `Assets/Firebase/Plugins/Firebase.Auth.dll` (hoặc tương đương trong package Firebase).

### B — Không dùng Auth

1. Trong `SDKSetup`: tắt **Firebase Auth**.
2. **Apply Settings to Scene** (gỡ `FIREBASE_AUTH`).
3. Hoặc xóa `FIREBASE_AUTH` khỏi **Player Settings → Scripting Define Symbols**.

### C — Google Sign-In (tùy chọn)

Import [Google Sign-In Unity](https://github.com/googlesamples/google-signin-unity) nếu dùng `SignInWithGoogleAsync`.  
**Gỡ** define `GOOGLE_SIGNIN` khỏi Scripting Define Symbols (không còn bắt buộc).  
Nếu chỉ dùng Play Games / Game Center / Anonymous, không cần plugin Google Sign-In.

## API sau khi cài Auth

```csharp
using JisSDKAds.Firebase;

await FirebaseManager.Instance.InitAsync();
FirebaseManager.Instance.FirebaseAuth.SignInWithPlatformAsync();

// Hoặc
FirebaseManager.Instance.FirebaseAuth.SignedIn += user => { };
```

`FirebaseAuthManager` (obsolete alias) vẫn hoạt động qua property `FirebaseAuthManager`.

## Không dùng Auth nhưng vẫn lỗi

- Cập nhật package `com.jis.sdkads.firebase` bản mới (assembly Auth tách riêng).
- Gỡ `FIREBASE_AUTH` khỏi Scripting Define Symbols (và `GOOGLE_SIGNIN` nếu còn từ bản cũ).
- Đảm bảo folder `Runtime/Auth` có file `.meta` (bản SDK mới đã có `Auth.meta`).
