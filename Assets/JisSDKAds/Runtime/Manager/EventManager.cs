using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
namespace SDK {
    public class EventManager : MonoBehaviour {
        private Dictionary<string, UnityEvent> eventDictionary;
        private List<UnityEvent> eventStack = new List<UnityEvent>();
        private Dictionary<UnityEvent, float> delayEventStackDictionary = new Dictionary<UnityEvent, float>();

        private static EventManager m_Instance;

        private static EventManager Instance => m_Instance;

        private void Awake() {
            if (m_Instance != null) {
                DestroyImmediate(gameObject);
            } else {
                m_Instance = this;
                m_Instance.Init();
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Init() {
            if (eventDictionary == null) {
                eventDictionary = new Dictionary<string, UnityEvent>();
            }
        }

        public static void StartListening(string eventName, UnityAction listener) {
            if (Instance == null) return;
            if (Instance.eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent)) {
                thisEvent.AddListener(listener);
            } else {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                Instance.eventDictionary.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, UnityAction listener) {
            if (m_Instance == null) return;
            if (Instance.eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent)) {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(string eventName) {
            if (Instance) {
                if (Instance.eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent)) {
                    thisEvent.Invoke();
                }
            }
        }
        public static void AddEventNextFrame(UnityAction listener) {
            UnityEvent thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            Instance.eventStack.Add(thisEvent);
        }
        public static void AddEventWithDelay(UnityAction listener, float delay) {
            UnityEvent thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            Instance.delayEventStackDictionary.Add(thisEvent, delay);
        }
        private void Update() {
            while (Instance.eventStack.Count > 0) {
                UnityEvent thisEvent = Instance.eventStack[0];
                thisEvent.Invoke();
                Instance.eventStack.RemoveAt(0);
            }
            foreach (KeyValuePair<UnityEvent, float> item in Instance.delayEventStackDictionary) {
                if (item.Value <= 0) {
                    item.Key.Invoke();
                    Instance.delayEventStackDictionary.Remove(item.Key);
                } else {
                    Instance.delayEventStackDictionary[item.Key] -= Time.deltaTime;
                }
            }
        }
    }
}
