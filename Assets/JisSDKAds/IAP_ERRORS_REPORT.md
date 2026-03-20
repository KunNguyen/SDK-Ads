# Báo cáo lỗi IAP (Purchase)

## Các lỗi đã phát hiện

### 1. **Nghiêm trọng: OnPurchaseFailed không gọi callback**

**File:** `IapCallbacks.cs` (dòng 90-97)

**Vấn đề:** Khi `OnPurchaseFailed(FailedOrder failedOrder)` được gọi, code chỉ log nhưng **không gọi** `InAppPurchaser.OnPurchaseFailed()`. Do đó:
- `OnBuyFailedCallback` không bao giờ được gọi
- Event `"BuyIAPFail"` không được trigger
- Event `"TurnOffLoading"` không được trigger
- Game không biết khi nào mua thất bại

**Sửa:** Thêm `InAppPurchaser.OnPurchaseFailed(cartItem.Product, reason)` trong vòng lặp.

---

### 2. **Typo: Quandtity → Quantity**

**File:** `IAPLogger.cs` (dòng 56)

**Vấn đề:** `Quandtity` viết sai, đúng là `Quantity`.

---

### 3. **NullReference: IapProductConfigs.Packages có thể null**

**File:** `InAppPurchaser.cs` (dòng 123)

**Vấn đề:** Chỉ kiểm tra `IapProductConfigs == null` nhưng không kiểm tra `IapProductConfigs.Packages`. Nếu `Packages` là null, `foreach` sẽ gây NullReferenceException.

---

### 4. **Tên file asset không nhất quán**

**File:** `IAPSetup.cs` (dòng 12)

**Vấn đề:** Tạo `IAPPackageConfigs` nhưng lưu với tên `IAPPackage.asset`. Nên đổi thành `IAPPackageConfigs.asset` cho rõ ràng.

---

### 5. **IAPPackageConfigs.Packages cần khởi tạo mặc định**

**File:** `IAPPackageConfigs.cs`

**Vấn đề:** `List<IAPPackage> Packages` có thể null khi tạo mới ScriptableObject. Nên thêm `= new List<IAPPackage>()` để tránh NullReference.

---

## Tóm tắt

| # | Mức độ | File | Mô tả |
|---|--------|------|-------|
| 1 | Nghiêm trọng | IapCallbacks.cs | OnPurchaseFailed không gọi callback |
| 2 | Thấp | IAPLogger.cs | Typo Quandtity |
| 3 | Trung bình | InAppPurchaser.cs | Thiếu null check Packages |
| 4 | Thấp | IAPSetup.cs | Tên file asset |
| 5 | Trung bình | IAPPackageConfigs.cs | Packages có thể null |
