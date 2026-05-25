# Local Notifications (com.jis.sdkads.notifications)

Wrapper around [Unity Mobile Notifications](https://docs.unity3d.com/Packages/com.unity.mobile.notifications@2.4/manual/index.html) for daily play reminders and gameplay timers (energy, shop reset, etc.).

## Install

1. **JIS SDK Hub** → **Local Notifications** → Import  
   (adds `com.jis.sdkads.notifications`, `com.jis.sdkads.common`, `com.unity.mobile.notifications`)
2. Or add to `Packages/manifest.json`:
   ```json
   "com.jis.sdkads.notifications": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.notifications#main"
   ```

## Quick setup

1. **JIS SDK → Notifications → Create Settings Asset**  
   Creates `Assets/Resources/JisLocalNotificationSettings.asset` (auto-loaded by manager).
2. Add to bootstrap scene, or at game start:
   ```csharp
   JisLocalNotificationManager.EnsureInstance();
   ```
3. On **Android 13+** / **iOS**, permission is requested on start (configurable).

## Daily play reminder (10 days)

Pre-schedules the next N local notifications at a fixed local time (default 19:00, 10 days). Refreshed when the app goes to background.

```csharp
var mgr = JisLocalNotificationManager.Instance;
mgr.ScheduleDailyPlayReminders(); // uses settings
mgr.ScheduleDailyPlayReminders(daysAhead: 10, hour: 20, minute: 30);
```

## Gameplay timers

```csharp
// Energy full in 2 hours
JisGameplayNotifications.ScheduleEnergyFull(mgr, TimeSpan.FromHours(2));

// Shop reset at local midnight
var reset = DateTime.Today.AddDays(1);
JisGameplayNotifications.ScheduleShopReset(mgr, reset);

// Daily reward expires tonight
JisGameplayNotifications.ScheduleDailyReward(mgr, DateTime.Today.AddHours(23));

// Cancel when player is in-game (avoid spam)
JisGameplayNotifications.CancelEnergyFull(mgr);
```

Custom:

```csharp
mgr.ScheduleAt(JisLocalNotificationIds.Custom("boss_respawn"), "Boss is back!", "Fight now.", fireTime);
mgr.Cancel(JisLocalNotificationIds.Custom("boss_respawn"));
```

## Permission

```csharp
mgr.RequestPermission(granted => Debug.Log("Notifications: " + granted));
mgr.PermissionChanged += status => { /* update UI */ };
```

## Platform notes

| Platform | Notes |
|----------|--------|
| **Android** | Notification channel registered on init. Optional small/large icons: add `icon_small` / `icon_large` under `Plugins/Android/res`. |
| **iOS** | Enable **Push Notifications** capability if you later add remote push; local notifications work with alert/badge/sound authorization. |
| **Editor** | No real notifications; APIs no-op safely. |

## Suggested extra features for idle / card games

| Feature | Idea |
|---------|------|
| **Streak / login chain** | Notify 1h before streak breaks if player has not opened today. |
| **Unclaimed daily reward** | `ScheduleDailyReward` a few hours before reset. |
| **Limited event ending** | `ScheduleEventEnding` 1h before event ends. |
| **Inactivity** | After 24h / 48h away, one gentle “come back” (use `ScheduleInactivityReminder`). |
| **Quiet hours** | Only schedule fires outside e.g. 22:00–08:00 local time (wrap in your game code). |
| **Deep link payload** | Extend platform layer with `IntentData` / `iOSNotification.Data` to open shop or battle screen on tap. |
| **Cancel on login** | Call `CancelEnergyFull` / `CancelShopReset` when timers are no longer relevant. |

## Module dependency

- `com.jis.sdkads.common` (ScriptOrder helper)
- `com.unity.mobile.notifications` 2.4.3+
