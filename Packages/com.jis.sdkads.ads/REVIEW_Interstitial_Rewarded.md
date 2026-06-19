# Review hệ thống Interstitial & Rewarded — `com.jis.sdkads.ads`

Phạm vi review: `JisAds` (orchestrator, ~2465 dòng) và `AdManager` (Core, ~558 dòng), kèm các luồng phụ trợ `AdLoadCoordinator`, `SequentialTierLoader`, `AdEvents`.

---

## 1. Điểm mạnh — đã làm tốt

1. **Tách lớp rõ ràng**
   - `JisAds` lo policy: capping, tracking, banner-restore, preload strategy.
   - `AdManager` lo cơ chế thuần: load → show → fallback → retry.
2. **Re-entrancy guard** — `_interstitialCallbacksInFlight` / `_rewardedCallbacksInFlight` chặn show kép, kèm `_*ShowAttemptId` để watchdog không lẫn giữa các lần show.
3. **In-flight watchdog 60s** (`CoInterstitialInFlightWatchdog`, `CoRewardedInFlightWatchdog`) — recovery quan trọng khi SDK quên gọi `onClosed`/`onFailed`.
4. **Capping interstitial 2 tầng**: theo thời gian từ app-open + giữa các show. Rewarded thành công cũng reset cooldown — UX hợp lý.
5. **Preload có backoff & retry** (`HandlePreloadFailed` + `RetryBackoff`), kèm **fallback-first preload order** (`GetFullscreenPreloadProviderIds`) để luôn sẵn ad khi tier chính đang lader.
6. **Defer interstitial preload tới khi pipeline idle** (`CoDeferredInterstitialPreload`) — tránh đua tải với rewarded urgent.
7. **Multi-mediation (AdMob + MAX) với sequential tier ladder** — `DecorateSequentialAdsIfEnabled` rẽ nhánh per-provider.
8. **Tracking đầy đủ**: ClickOnButton, ShowSuccess, ShowFail, ShowFailByLoad, ShowCompleted.
9. **Banner restore sau fullscreen** debounce + retry.
10. **Remove Ads side-effects** (chặn preload + hide banner) gọn gàng.

---

## 2. Vấn đề & rủi ro nên xử lý

### 2.1. Mismatch attribution `OnCoreInterstitialShown` với `ShowInterstitialForResume`

`ShowInterstitialForResume` gọi thẳng `_core.ShowInterstitial(...)` **không** set `_interstitialCallbacksInFlight`, không set placement, không track. Nhưng vẫn raise `AdEvents.OnInterstitialShown` → `OnCoreInterstitialShown` check `_interstitialCallbacksInFlight`. Nếu resume chạy đồng thời với show bình thường in-flight, `_pendingInterstitialWasShown` và `TrackPendingInterstitialShowSuccess` có thể bị gán vào placement sai.

**Đề xuất**: gắn `attemptId` trong sự kiện `OnInterstitialShown`; hoặc cho `ShowInterstitialForResume` đi qua re-entrancy guard.

### 2.2. `lock(this)` ở `InitializeAsync`

```csharp
lock (this)
{
    if (_initializeAsyncGate == null) { ... }
    else { inFlight = _initializeAsyncGate.Task; }
}
```

`lock(this)` là anti-pattern. Dùng `private readonly object _initLock = new();`.

### 2.3. `SetAdsShowingState(true)` đặt không nhất quán

- Rewarded: set `true` ngay đầu hàm (trước khi check load).
- Interstitial: set `true` **sau** khi đã check load thành công.

Bất nhất hard-to-reason; nên thống nhất: chỉ set `true` khi thực sự sắp gọi `_core.Show*`.

### 2.4. `AdManager.TryShowInterstitialWithFallback` lặp Load mỗi attempt

JisAds đã sớm fail nếu chưa có ad loaded, nên branch này thường chỉ chạy khi gọi thẳng `_core.ShowInterstitial` (resume). Lúc đó có thể block 3 retries × Load × 2s. Resume nên có timeout/giới hạn riêng.

### 2.5. `ShowInterstitialForResume` không dùng watchdog, không hide banner

Nếu callback `_core.ShowInterstitial` không bao giờ về, không có recovery. Banner còn nằm trên màn hình → overlap click với ad. Nên wrap watchdog + `HideBannerForFullscreenAd` + `SetAdsShowingState(true)`.

### 2.6. Delay invoke `showSuccessCallback` tới lúc close

```csharp
if (didShow) onSuccess?.Invoke();
onClosed?.Invoke();
```

Nhiều game gắn pause-music / dim-screen vào `showSuccessCallback`. Trễ tới khi close làm hành vi không khớp tên callback. Cân nhắc cung cấp callback `onPresented` riêng được gọi đúng lúc `OnInterstitialShown`.

### 2.7. Mất reward khi watchdog fail sớm hơn `OnRewardEarned`

Nếu watchdog fire trước rồi `OnCoreRewardEarned` mới đến (SDK lag), người chơi đã xem hết quảng cáo mà game đã gọi `failedCallback` → mất phần thưởng. Cân nhắc: nếu `OnCoreRewardEarned` đến mà callbacks đã clear, vẫn cấp reward trễ qua event global.

### 2.8. Watchdog dùng `WaitForSecondsRealtime` + pause

Trên iOS / một số Unity build, `realtime` có thể vẫn đếm khi background. Pause 70s → watchdog có thể fire ngay khi quay lại, fail-giả trong khi ad vẫn đang trên màn hình. Cân nhắc reset watchdog ở `OnApplicationPause(true)`.

### 2.9. Error info quá generic

`AdEvents.OnInterstitialFailed` chỉ truyền `err` string — không có error code, không có mediation name. Khó build dashboard analytics theo provider × error type. Nên đính kèm `providerId` và enum lỗi (NoFill / NetworkError / InternalError / Timeout).

### 2.10. `IsInterstitialAdLoaded()` cấp phát mỗi call

```csharp
public bool IsInterstitialAdLoaded() =>
    IsInterstitialAdLoaded(BuildFullscreenShowProviderOrder(AdsMediationType.NONE, AdFormat.Interstitial));
```

Game thường poll mỗi frame để bật/tắt nút. Nên cache provider-order, hoặc trả về readonly span/cached list. Tương tự `IsRewardedVideoLoaded()`.

### 2.11. Vận hành

- `InitializeCoreFlow` mất state khi failure: `useCoreForStandardFormats=false` vĩnh viễn, không retry init Core.
- `_failedProviderErrors` (Core) chưa được surface ra JisAds — giúp debug nhanh hơn.

---

## 3. Tổng kết mức độ

| Khía cạnh | Điểm |
|---|---|
| Kiến trúc & tách lớp | 9/10 |
| Resilience (watchdog, retry, fallback) | 8.5/10 |
| Capping & UX policy | 8/10 |
| Tracking | 8/10 |
| Race-condition / Reentrancy | 7/10 |
| Quan sát & lỗi chi tiết | 6.5/10 |
| Hiệu năng GC | 7/10 |

**Tổng thể: production-ready** — vượt xa SDK in-house thông thường nhờ watchdog + sequential tier + multi-mediation orchestration.

---

## 4. Kịch bản interstitial KHÔNG THỂ LOAD

Đây là phần quan trọng — các trường hợp interstitial có thể "kẹt", không bao giờ load tới khi user kill app hoặc trigger thủ công.

### 4.1. RC fail/chậm + không có fallback timeout — **RỦI RO CAO**

```csharp
if (!IsRemoteConfigReady())
{
    _pendingFullscreenPreloadAfterRemoteConfig = true;
    DebugAds.Log("[JisAds] Deferring interstitial/rewarded preload — Remote Config not ready.");
    return;
}
```

Preload interstitial **bị block hoàn toàn** tới khi `FirebaseManager.IsRemoteConfigReady == true`. Nếu user offline + Firebase fetch fail + `IsRemoteConfigReady` không set khi fail → cờ kẹt mãi. Không có watchdog "RC chưa ready trong N giây thì cứ load với defaults".

### 4.2. Sequential tier "permanently gives up" — **RỦI RO TRUNG BÌNH–CAO**

Comment chính chủ trong code:

> *"the tiered preload retry stops after a few "no ad unit configured" failures"*

Tier ladder dừng vĩnh viễn sau vài fail. Sau đó interstitial chết tới khi RC refresh re-arm. Nếu RC không refresh trong session, interstitial ngủ luôn.

### 4.3. Core init fail → flip false vĩnh viễn — **RỦI RO TRUNG BÌNH**

```csharp
onFailure: err =>
{
    useCoreForStandardFormats = false;
    _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
    _pendingImmediatePreloadAfterCoreReady = false;
    _pendingFullscreenPreloadAfterRemoteConfig = false;
});
```

Không retry init Core trong session. Mọi `ShowInterstitial` rơi vào nhánh `"Legacy interstitial is removed"` và fail.

### 4.4. Provider init fail không retry — **RỦI RO TRUNG BÌNH**

```csharp
if (!_core.IsProviderInitialized(providerId))
{
    DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=provider_not_initialized");
    continue;
}
```

Dựa vào `HandleProviderInitialized` event sau đó. Nhưng nếu provider init **fail** (không bao giờ raise `OnProviderInitialized`), không có second chance. JisAds không subscribe `AdEvents.OnProviderFailed`.

### 4.5. Backoff vô hạn khi NoFill liên tục — **RỦI RO TRUNG BÌNH**

`HandlePreloadFailed` tăng `_preloadFailCounts[Interstitial]` mỗi lần fail. Nếu `RetryBackoff` không cap delay, sau N fail (NoFill liên tục) delay rất lớn (>10 phút) — thực tế đồng nghĩa "không load được" cả session.

Cần kiểm tra `RetryBackoff.GetDelaySeconds` có cap không, và **reset `_preloadFailCounts` khi network state thay đổi**.

### 4.6. Loaded-ad expire không invalidate — **RỦI RO TRUNG BÌNH (session dài)**

Chỉ check flag `provider.Interstitial.IsLoaded`. AdMob/MAX có TTL ~1h — sau expire, `IsLoaded` vẫn true. Preload pipeline skip với `reason=already_loaded` và không load lại. Show fail mới invalidate.

### 4.7. Remove-ads revoke không reset flag preload — **RỦI RO THẤP**

`ApplyRemoveAdsSideEffects` set `_immediateFormatsPreloadedOnCoreReady = true` & `_fullscreenFormatsPreloadedAfterRemoteConfig = true`. Khi `SetRemoveAds(false)` (refund/restore reverse), các flag này không reset → preload startup không chạy lại.

### 4.8. `_interstitialCallbacksInFlight` kẹt — **RỦI RO THẤP**

Watchdog 60s sẽ recover, nhưng trong 60s đó interstitial không thể show/load qua user action.

### 4.9. Stack Load() trùng — **RỦI RO THẤP**

`RequestInterstitialLoadIfNeeded` → `PreloadInterstitialAd` không có cờ "load in-flight" như banner. Có thể stack nhiều Load() callback. Tracking nhiễu, `_preloadFailCounts` có thể tăng nhầm.

---

## 5. Bảng xếp hạng rủi ro load

| # | Kịch bản | Xác suất | Hậu quả |
|---|---|---|---|
| 4.1 | RC không ready → preload không bao giờ chạy | **Cao** (user offline mở app) | Interstitial chết cả session |
| 4.2 | Sequential tier "permanently gives up" | Trung bình–Cao | Chết tới khi RC refresh |
| 4.3 | Core init fail → flip false vĩnh viễn | Trung bình | Chết cả session |
| 4.4 | Provider init fail không retry | Trung bình | Chết theo provider |
| 4.5 | Backoff vô hạn khi NoFill liên tục | Trung bình | Delay rất lớn |
| 4.6 | Loaded-ad expire không invalidate | Trung bình (session dài) | Show fail rồi mới load lại |
| 4.7 | Remove-ads revoke không reset flag preload | Thấp | Preload startup mất |
| 4.8 | `_interstitialCallbacksInFlight` kẹt | Thấp | Tối đa 60s tới watchdog |
| 4.9 | Stack Load() trùng | Thấp | Tracking nhiễu |

---

## 6. Đề xuất sửa ưu tiên (impact / cost)

1. **Timeout "RC must-be-ready" (~10s)** cho `TryPreloadFullscreenFormatsAfterRemoteConfig`: sau timeout vẫn preload với cấu hình mặc định, bypass RC. → Vá #4.1.
2. **Subscribe `Application.internetReachabilityChanged`** (hoặc poll khi `OnApplicationFocus`) để reset `_preloadFailCounts` và gọi `RequestInterstitialLoadIfNeeded`/`RequestRewardedLoadIfNeeded`. → Vá #4.5, #4.4, #4.6.
3. **Subscribe `AdEvents.OnProviderFailed`** và lên lịch retry init provider với backoff. → Vá #4.4, #4.3.
4. **Thêm cờ `_interstitialLoadInFlight` / `_rewardedLoadInFlight`** giống `_bannerPreloadInFlight`. → Vá #4.9.
5. **Reset preload flags khi `SetRemoveAds(false)`**. → Vá #4.7.
6. **Public API `JisAds.ForceRearmInterstitialLoad()`** cho game gọi từ chỗ quan trọng (vào màn level) — phòng khi tier permanently give up. → Vá #4.2 thực dụng.
7. **Track tuổi của loaded ad** (timestamp khi `onLoaded`), treat `IsLoaded` là false nếu > 50 phút; preload đè. → Vá #4.6 triệt để.
8. **Gắn `attemptId` vào `AdEvents.OnInterstitialShown`** + cho `ShowInterstitialForResume` đi qua guard chung. → Vá #2.1, #2.5.
9. **Đính `providerId` + enum lỗi** vào các event `*Failed`. → Vá #2.9.
10. **Cache provider-order** cho `IsInterstitialAdLoaded()` / `IsRewardedVideoLoaded()`. → Vá #2.10.

**Ưu tiên P0 (làm trước)**: #1, #2, #6 — chiếm phần lớn tác động và sửa nhẹ nhàng.
**Ưu tiên P1**: #3, #7, #8.
**Ưu tiên P2**: #4, #5, #9, #10.
