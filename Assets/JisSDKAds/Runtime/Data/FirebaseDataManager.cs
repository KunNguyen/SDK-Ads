#if Firebase_Database
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FirebaseDataManager : MonoBehaviour {
    private static FirebaseDataManager m_Instance;
    public static FirebaseDataManager Instance {
        get {
            return m_Instance;
        }
    }
    public DatabaseReference m_DatabaseReference;
    private void Awake() {
        m_Instance = this;
    }
    public void Setup() {
        Debug.Log("Setup Data");

        m_DatabaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        m_DatabaseReference.ValueChanged += HandleValueChanged;
        m_DatabaseReference.ChildAdded += HandleChildAdded;
        m_DatabaseReference.ChildChanged += HandleChildChanged;
        m_DatabaseReference.ChildRemoved += HandleChildRemoved;
        m_DatabaseReference.ChildMoved += HandleChildMoved;

        GiftCodeManager.Instance.Test();
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        // Do something with the data in args.Snapshot
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        // Do something with the data in args.Snapshot
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        // Do something with the data in args.Snapshot
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        // Do something with the data in args.Snapshot
    }

    void HandleChildMoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        // Do something with the data in args.Snapshot
    }

} 
#endif