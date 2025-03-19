using UnityEngine;
using System.Collections.Generic;

public class CasiotoneKeyLayout : MonoBehaviour
{
    [Header("Keyboard Settings")]
    public float keyboardStartOffset = 0.05f;  // Offset from left edge
    public float keyboardWidth = 0.85f;        // Active keyboard area width
    
    [Header("Key Dimensions")]
    public float whiteKeyWidth = 0.0225f;
    public float whiteKeyLength = 0.14f;
    public float blackKeyWidth = 0.012f;
    public float blackKeyLength = 0.09f;
    
    [Header("References")]
    public GameObject whiteKeyPrefab;
    public GameObject blackKeyPrefab;
    public Transform keysParent;
    
    // MIDI note to key object mapping
    private Dictionary<int, GameObject> keyObjects = new Dictionary<int, GameObject>();
    
    public void CreateKeyboardLayout()
    {
        if (keysParent == null || whiteKeyPrefab == null || blackKeyPrefab == null)
        {
            Debug.LogError("Missing references for key layout");
            return;
        }
        
        // Clear any existing keys
        foreach (Transform child in keysParent)
        {
            Destroy(child.gameObject);
        }
        
        // Starting MIDI note for 61-key keyboard (C2 = MIDI note 36)
        int startNote = 36;
        int endNote = 96; // 61 keys: 36 to 96
        
        // Position of leftmost key
        float startX = -keyboardWidth/2 + keyboardStartOffset;
        float keyboardSurfaceHeight = 0.01f;  // Slight offset above piano
        
        // Track white key positions
        float[] whiteKeyPositions = new float[36];  // 36 white keys in 61-key keyboard
        int whiteKeyCount = 0;
        
        // Create white keys first - REVERSED ORDER
        float currentX = startX;
        
        // First, find all white keys and their positions
        for (int i = 0; i < 61; i++)
        {
            int midiNote = endNote - i; // Start from highest note (reversed)
            int noteInOctave = midiNote % 12;
            
            // Check if it's a white key
            if (noteInOctave == 0 || noteInOctave == 2 || noteInOctave == 4 || 
                noteInOctave == 5 || noteInOctave == 7 || noteInOctave == 9 || 
                noteInOctave == 11)
            {
                // Create white key
                GameObject keyObj = Instantiate(whiteKeyPrefab, keysParent);
                keyObj.name = $"WhiteKey_{midiNote}";
                
                // Position key
                keyObj.transform.localPosition = new Vector3(currentX, keyboardSurfaceHeight, 0);
                
                // Store position for black keys
                whiteKeyPositions[whiteKeyCount] = currentX;
                whiteKeyCount++;
                
                // Store reference for MIDI highlighting
                keyObjects[midiNote] = keyObj;
                
                // Move to next white key
                currentX += whiteKeyWidth;
            }
        }
        
        // Now add black keys - REVERSED ORDER
        for (int i = 0; i < 61; i++)
        {
            int midiNote = endNote - i; // Start from highest note (reversed)
            int noteInOctave = midiNote % 12;
            
            // Check if it's a black key
            if (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || 
                noteInOctave == 8 || noteInOctave == 10)
            {
                // Calculate position - this is more complex since we've reversed the order
                // We need to find the neighboring white keys
                int reversedIndex = 60 - i; // Index in the reversed list
                int octave = (midiNote - startNote) / 12;
                float xPos = 0;
                
                // Find the correct position based on the adjacent white keys
                // This part needs careful adjustment since our white keys are now reversed
                if (noteInOctave == 1) // C# - between C and D
                {
                    int cIndex = FindWhiteKeyIndex(midiNote - 1, endNote);
                    if (cIndex >= 0 && cIndex < whiteKeyCount)
                        xPos = whiteKeyPositions[cIndex] + whiteKeyWidth * 0.7f;
                }
                else if (noteInOctave == 3) // D# - between D and E
                {
                    int dIndex = FindWhiteKeyIndex(midiNote - 1, endNote);
                    if (dIndex >= 0 && dIndex < whiteKeyCount)
                        xPos = whiteKeyPositions[dIndex] + whiteKeyWidth * 0.7f;
                }
                else if (noteInOctave == 6) // F# - between F and G
                {
                    int fIndex = FindWhiteKeyIndex(midiNote - 1, endNote);
                    if (fIndex >= 0 && fIndex < whiteKeyCount)
                        xPos = whiteKeyPositions[fIndex] + whiteKeyWidth * 0.7f;
                }
                else if (noteInOctave == 8) // G# - between G and A
                {
                    int gIndex = FindWhiteKeyIndex(midiNote - 1, endNote);
                    if (gIndex >= 0 && gIndex < whiteKeyCount)
                        xPos = whiteKeyPositions[gIndex] + whiteKeyWidth * 0.7f;
                }
                else if (noteInOctave == 10) // A# - between A and B
                {
                    int aIndex = FindWhiteKeyIndex(midiNote - 1, endNote);
                    if (aIndex >= 0 && aIndex < whiteKeyCount)
                        xPos = whiteKeyPositions[aIndex] + whiteKeyWidth * 0.7f;
                }
                
                // Create black key if we have a valid position
                if (xPos != 0)
                {
                    GameObject keyObj = Instantiate(blackKeyPrefab, keysParent);
                    keyObj.name = $"BlackKey_{midiNote}";
                    
                    // Position key (black keys are further back)
                    keyObj.transform.localPosition = new Vector3(xPos, keyboardSurfaceHeight, -whiteKeyLength/2 + blackKeyLength/2);
                    
                    // Store reference
                    keyObjects[midiNote] = keyObj;
                }
            }
        }
        
        Debug.Log("Created keyboard layout with 61 keys in reversed order");
    }
    
    // Helper method to find the index of a white key in our array
    private int FindWhiteKeyIndex(int midiNote, int highestNote)
    {
        int count = 0;
        
        // Count how many white keys exist from the highest note down to this note
        for (int note = highestNote; note >= midiNote; note--)
        {
            int noteInOctave = note % 12;
            
            if (noteInOctave == 0 || noteInOctave == 2 || noteInOctave == 4 || 
                noteInOctave == 5 || noteInOctave == 7 || noteInOctave == 9 || 
                noteInOctave == 11)
            {
                if (note == midiNote)
                    return count;
                    
                count++;
            }
        }
        
        return -1; // Not found
    }

    // Method to get a key GameObject by MIDI note
    public GameObject GetKeyObject(int midiNote)
    {
        if (keyObjects.ContainsKey(midiNote))
            return keyObjects[midiNote];
        return null;
    }
    
    // Method to highlight a key when played
    public void HighlightKey(int midiNote, Color color)
    {
        if (keyObjects.ContainsKey(midiNote) && keyObjects[midiNote] != null)
        {
            MeshRenderer keyRenderer = keyObjects[midiNote].GetComponent<MeshRenderer>();
            if (keyRenderer != null)
            {
                keyRenderer.material.color = color;
            }
        }
    }

    // Method to reset all keys to default colors
    public void ResetAllKeys()
    {
        foreach (var pair in keyObjects)
        {
            GameObject keyObj = pair.Value;
            if (keyObj != null)
            {
                MeshRenderer renderer = keyObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // Determine if it's a white or black key
                    int midiNote = pair.Key;
                    int noteInOctave = midiNote % 12;
                    bool isBlackKey = (noteInOctave == 1 || noteInOctave == 3 || 
                                    noteInOctave == 6 || noteInOctave == 8 || 
                                    noteInOctave == 10);
                    
                    // Reset to default color
                    if (isBlackKey)
                        renderer.material.color = Color.black;
                    else
                        renderer.material.color = Color.white;
                }
            }
        }
    }
}