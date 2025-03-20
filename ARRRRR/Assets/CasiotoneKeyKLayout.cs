using UnityEngine;
using System.Collections.Generic;

public class CasiotoneKeyLayout : MonoBehaviour
{
    [Header("Keyboard Settings")]
    public float keyboardStartOffset = 0.015f;  // Offset from edge
    public float keyboardWidth = 0.85f;        // Total width of piano
    
    [Header("Key Dimensions")]
    public float whiteKeyWidth = 0.03f;      // 3cm per your measurements
    public float whiteKeyLength = 0.13f;     // 13cm per your measurements
    public float blackKeyWidth = 0.015f;     // Half of white key width
    public float blackKeyLength = 0.085f;    // About 2/3 of white key length
    
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
        keyObjects.Clear();
        
        // 61-key keyboard (C2 = MIDI note 36 to C7 = MIDI note 96)
        int lowNote = 36;  // C2
        int highNote = 96; // C7
        
        // Calculate total keyboard width
        int totalWhiteKeys = 0;
        for (int note = lowNote; note <= highNote; note++)
        {
            int noteInOctave = note % 12;
            if (noteInOctave == 0 || noteInOctave == 2 || noteInOctave == 4 || 
                noteInOctave == 5 || noteInOctave == 7 || noteInOctave == 9 || 
                noteInOctave == 11)
            {
                totalWhiteKeys++;
            }
        }
        
        Debug.Log($"Total white keys: {totalWhiteKeys}");
        
        // Position of rightmost key (reversed layout)
        float startX = keyboardWidth/2 - keyboardStartOffset;
        float keyboardSurfaceHeight = 0.001f;  // Slight offset above piano
        float currentX = startX;
        
        // Create white keys first from right to left (low to high notes, reversed)
        Dictionary<int, float> whiteKeyPositions = new Dictionary<int, float>();
        
        for (int note = lowNote; note <= highNote; note++)
        {
            int noteInOctave = note % 12;
            
            // Check if it's a white key
            if (noteInOctave == 0 || noteInOctave == 2 || noteInOctave == 4 || 
                noteInOctave == 5 || noteInOctave == 7 || noteInOctave == 9 || 
                noteInOctave == 11)
            {
                // Create white key
                GameObject keyObj = Instantiate(whiteKeyPrefab, keysParent);
                keyObj.name = $"WhiteKey_{note}";
                
                // Position key
                keyObj.transform.localPosition = new Vector3(currentX, keyboardSurfaceHeight, 0);
                keyObj.transform.localScale = new Vector3(whiteKeyWidth * 0.95f, 0.01f, whiteKeyLength);
                
                // Store position for black keys
                whiteKeyPositions[note] = currentX;
                
                // Store reference for MIDI highlighting
                keyObjects[note] = keyObj;
                
                // Move to next white key (going leftward)
                currentX -= whiteKeyWidth;
            }
        }
        
        // Now create black keys
        for (int note = lowNote; note <= highNote; note++)
        {
            int noteInOctave = note % 12;
            
            // Check if it's a black key
            if (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || 
                noteInOctave == 8 || noteInOctave == 10)
            {
                // Get positions of adjacent white keys
                int leftWhiteKey = note - 1;  // Note: in reversed layout, this is physically on the right
                int rightWhiteKey = 0;        // Note: in reversed layout, this is physically on the left
                
                // Find the next white key to the right
                for (int n = note + 1; n <= highNote; n++)
                {
                    int nInOctave = n % 12;
                    if (nInOctave == 0 || nInOctave == 2 || nInOctave == 4 || 
                        nInOctave == 5 || nInOctave == 7 || nInOctave == 9 || 
                        nInOctave == 11)
                    {
                        rightWhiteKey = n;
                        break;
                    }
                }
                
                // Only create if we have both adjacent white keys
                if (whiteKeyPositions.ContainsKey(leftWhiteKey) && whiteKeyPositions.ContainsKey(rightWhiteKey))
                {
                    float rightPos = whiteKeyPositions[leftWhiteKey];    // In reversed layout, the "left" white key is physically on the right
                    float leftPos = whiteKeyPositions[rightWhiteKey];    // In reversed layout, the "right" white key is physically on the left
                    
                    // Position black key between white keys (slightly offset depending on which black key)
                    float offset = 0.4f; // Default offset from right white key (in reversed layout)
                    
                    if (noteInOctave == 1) offset = 0.3f;      // C#
                    else if (noteInOctave == 3) offset = 0.35f; // D#
                    else if (noteInOctave == 6) offset = 0.4f;  // F#
                    else if (noteInOctave == 8) offset = 0.35f; // G#
                    else if (noteInOctave == 10) offset = 0.3f; // A#
                    
                    float whiteKeySpacing = rightPos - leftPos;
                    float xPos = rightPos - (whiteKeySpacing * offset);
                    
                    // Create black key
                    GameObject keyObj = Instantiate(blackKeyPrefab, keysParent);
                    keyObj.name = $"BlackKey_{note}";
                    
                    // Position key
                    float zPos = -whiteKeyLength * 0.35f; // Position from the front edge
                    keyObj.transform.localPosition = new Vector3(xPos, keyboardSurfaceHeight + 0.001f, zPos);
                    keyObj.transform.localScale = new Vector3(blackKeyWidth, 0.015f, blackKeyLength);
                    
                    // Store reference
                    keyObjects[note] = keyObj;
                }
            }
        }
        
        Debug.Log("Created keyboard layout with 61 keys in reversed orientation (low notes on right)");
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