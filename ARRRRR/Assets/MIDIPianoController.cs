using UnityEngine;
using System.Collections.Generic;
using MidiJack;

public class MIDIPianoController2 : MonoBehaviour
{
    [Header("References")]
    public CasiotoneKeyLayout keyLayout;
    
    [Header("Key Colors")]
    public Material whiteKeyMaterial; // Assign the original white key material
    public Material blackKeyMaterial; // Assign the original black key material
    public Color keyPressedColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);
    
    // Track active notes
    private HashSet<int> activeNotes = new HashSet<int>();
    
    void Start()
    {
        // Register MIDI event callbacks
        MidiMaster.noteOnDelegate += OnNoteOn;
        MidiMaster.noteOffDelegate += OnNoteOff;
        
        Debug.Log("MIDI listener started. Waiting for input from Casiotone CT-S300...");
    }
    
    void OnDestroy()
    {
        // Unregister MIDI event callbacks
        MidiMaster.noteOnDelegate -= OnNoteOn;
        MidiMaster.noteOffDelegate -= OnNoteOff;
    }
    
    void OnNoteOn(MidiChannel channel, int note, float velocity)
    {
         Debug.Log($"MIDI Note On: Channel {channel}, Note {note}, Velocity {velocity}");
    
        // Regular key highlighting for normal play mode
        if (keyLayout != null)
        {
            keyLayout.HighlightKey(note, keyPressedColor);
            activeNotes.Add(note);
        }
    }
    
    void OnNoteOff(MidiChannel channel, int note)
    {
        // Debug.Log($"MIDI Note Off: Channel {channel}, Note {note}");
        
        if (keyLayout != null && activeNotes.Contains(note))
        {
            // Determine if white or black key
            int noteInOctave = note % 12;
            bool isBlackKey = (noteInOctave == 1 || noteInOctave == 3 || 
                              noteInOctave == 6 || noteInOctave == 8 || 
                              noteInOctave == 10);
            
            // Get the key GameObject
            GameObject keyObject = keyLayout.GetKeyObject(note);
            if (keyObject != null)
            {
                MeshRenderer keyRenderer = keyObject.GetComponent<MeshRenderer>();
                if (keyRenderer != null)
                {
                    // Restore original material
                    keyRenderer.material = isBlackKey ? blackKeyMaterial : whiteKeyMaterial;
                }
            }
            
            activeNotes.Remove(note);
        }
    }
    
    // For testing in the editor without a MIDI device
    void Update()
    {
        // Uncomment for keyboard testing
        /*
        // Computer keyboard testing for MIDI input
        if (Input.GetKeyDown(KeyCode.Z)) SimulateNoteOn(60);
        if (Input.GetKeyUp(KeyCode.Z)) SimulateNoteOff(60);
        
        if (Input.GetKeyDown(KeyCode.S)) SimulateNoteOn(61);
        if (Input.GetKeyUp(KeyCode.S)) SimulateNoteOff(61);
        
        if (Input.GetKeyDown(KeyCode.X)) SimulateNoteOn(62);
        if (Input.GetKeyUp(KeyCode.X)) SimulateNoteOff(62);
        */
    }
    
    // Helper methods for testing
    void SimulateNoteOn(int note)
    {
        OnNoteOn(MidiChannel.All, note, 1.0f);
    }
    
    void SimulateNoteOff(int note)
    {
        OnNoteOff(MidiChannel.All, note);
    }
}