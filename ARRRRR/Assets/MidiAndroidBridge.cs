using UnityEngine;
using UnityEngine.UI; // Added for UI Text component

public class MidiAndroidBridge : MonoBehaviour {
    AndroidJavaObject midiUnityPlugin; // Fixed variable name from MidiUnityPlugin to midiPlugin
    
    public Text midiDisplayText; // Reference to UI Text component
    
    void Awake() {
        midiDisplayText.text = "does this work with awake?";
    }

    void Start() {
        using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.example.MidiUnityPlugin")) {
            var midiPlugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
        }
        
            midiDisplayText.text = "DOES THIS WORK?";

        // Check if text component is assigned
        if (midiDisplayText == null) {
            Debug.LogError("Please assign a UI Text component to the midiDisplayText field!");
        }
    }

    void ReadMidi() {
        int note = midiUnityPlugin.Call<int>("NoteOn");
        
        // Update UI Text instead of using Debug.Log
        if (midiDisplayText != null) {
            midiDisplayText.text = "MIDI Note: " + note;
        }
    }
    
    // Optional: Add a method to call ReadMidi periodically or from an event
    void Update() {
        // You could call ReadMidi() here if you want to constantly update
        // ReadMidi();
        ReadMidi();
        // Or you might want to call it based on user input or other events
    }
}