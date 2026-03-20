using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SDK
{
    public class EventManager : MonoBehaviour
    {
        private static EventManager instance;
        public static EventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject("EventManager");
                    instance = obj.AddComponent<EventManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        // ==== Core Event Dictionaries ====
        private readonly Dictionary<string, UnityEvent> eventTable = new();
        private readonly Dictionary<string, object> eventTableGeneric = new();

        // ==== Delayed and Queued Events ====
        private readonly Queue<UnityEvent> nextFrameEvents = new();
        private readonly List<(UnityEvent evt, float delay)> delayedEvents = new();

        // ===============================
        #region --- Lifecycle ---
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Run next-frame events
            while (nextFrameEvents.Count > 0)
            {
                var evt = nextFrameEvents.Dequeue();
                if (evt == null) continue;

                try
                {
                    evt.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }

            // Run delayed events
            for (int i = delayedEvents.Count - 1; i >= 0; i--)
            {
                var (evt, delay) = delayedEvents[i];
                delay -= Time.deltaTime;

                if (delay <= 0f)
                {
                    delayedEvents.RemoveAt(i);

                    if (evt == null) continue;

                    try
                    {
                        evt.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, this);
                    }
                }
                else
                {
                    delayedEvents[i] = (evt, delay);
                }
            }
        }
        #endregion

        // ===============================
        #region --- Basic Events (no params) ---
        public static void StartListening(string eventName, UnityAction listener)
        {
            if (!Instance.eventTable.TryGetValue(eventName, out var evt))
            {
                evt = new UnityEvent();
                Instance.eventTable[eventName] = evt;
            }
            evt.AddListener(listener);
        }

        public static void StopListening(string eventName, UnityAction listener)
        {
            if (Instance.eventTable.TryGetValue(eventName, out var evt))
                evt.RemoveListener(listener);
        }

        public static void Trigger(string eventName)
        {
            if (Instance.eventTable.TryGetValue(eventName, out var evt))
                evt.Invoke();
#if UNITY_EDITOR
            else
                Debug.LogWarning($"[EventManager] No listeners for event '{eventName}'");
#endif
        }
        #endregion

        // ===============================
        #region --- Generic Events (with parameter) ---
        public static void StartListening<T>(string eventName, UnityAction<T> listener)
        {
            if (!Instance.eventTableGeneric.TryGetValue(eventName, out var obj))
            {
                var evt = new UnityEvent<T>();
                evt.AddListener(listener);
                Instance.eventTableGeneric[eventName] = evt;
            }
            else if (obj is UnityEvent<T> evt)
            {
                evt.AddListener(listener);
            }
            else
            {
                Debug.LogError($"[EventManager] Event '{eventName}' has conflicting type!");
            }
        }

        public static void StopListening<T>(string eventName, UnityAction<T> listener)
        {
            if (Instance.eventTableGeneric.TryGetValue(eventName, out var obj) && obj is UnityEvent<T> evt)
                evt.RemoveListener(listener);
        }

        public static void Trigger<T>(string eventName, T param)
        {
            if (Instance.eventTableGeneric.TryGetValue(eventName, out var obj) && obj is UnityEvent<T> evt)
                evt.Invoke(param);
#if UNITY_EDITOR
            else
                Debug.LogWarning($"[EventManager] No generic listeners for event '{eventName}'");
#endif
        }
        #endregion

        // ===============================
        #region --- One-shot Events ---
        public static void AddOneShot(string eventName, UnityAction listener)
        {
            UnityAction wrapper = null;
            wrapper = () =>
            {
                listener.Invoke();
                StopListening(eventName, wrapper);
            };
            StartListening(eventName, wrapper);
        }

        public static void AddOneShot<T>(string eventName, UnityAction<T> listener)
        {
            UnityAction<T> wrapper = null;
            wrapper = (param) =>
            {
                listener.Invoke(param);
                StopListening(eventName, wrapper);
            };
            StartListening(eventName, wrapper);
        }
        #endregion

        // ===============================
        #region --- Next Frame / Delayed ---
        public static void InvokeNextFrame(UnityAction listener)
        {
            var evt = new UnityEvent();
            evt.AddListener(listener);
            Instance.nextFrameEvents.Enqueue(evt);
        }

        public static void InvokeDelayed(UnityAction listener, float delay)
        {
            var evt = new UnityEvent();
            evt.AddListener(listener);
            Instance.delayedEvents.Add((evt, delay));
        }
        #endregion

        // ===============================
        #region --- Utility ---
        public static void ClearAll()
        {
            Instance.eventTable.Clear();
            Instance.eventTableGeneric.Clear();
            Instance.nextFrameEvents.Clear();
            Instance.delayedEvents.Clear();
        }
        #endregion
    }
}