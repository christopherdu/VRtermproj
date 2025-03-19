using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MidiJack;
using System.IO;
using System.Diagnostics;

public class MIDIPianoGame2 : MonoBehaviour
{
    [Header("References")]
    public CasiotoneKeyLayout keyLayout;
    public GameObject arrow; // Reference to the arrow GameObject

    [Header("Highlight Colors")]
    public Color keyHighlightColor = Color.red;

    private int currentKeyIndex = 36; // Start at MIDI note 36 (C2)
    private int activeKey = -1; // Only one active key at a time
    private float keyHighlightTime;
    private string csvFilePath = "";

    void Start()
    {
        // Set CSV file path
        csvFilePath = Application.dataPath + "/reaction_times.csv";

        // Create the CSV file and write headers if it doesn't exist
        if (!File.Exists(csvFilePath))
        {
            WriteToCSV("MIDI Note,Key Highlight Time,Key Press Time,Reaction Time (Seconds),Accuracy,Timestamp", true);
        }

        // Open CSV file only once at the start (optional in editor mode)
#if UNITY_EDITOR
        OpenCSVFile();
#endif

        StartCoroutine(WaitForKeysAndHighlight());
    }

    IEnumerator WaitForKeysAndHighlight()
    {
        while (keyLayout.GetKeyObject(currentKeyIndex) == null)
        {
            UnityEngine.Debug.Log("Waiting for keyboard to initialize...");
            yield return null;
        }

        UnityEngine.Debug.Log("Keyboard initialized, highlighting first key.");
        HighlightNextKey(currentKeyIndex);
        MidiMaster.noteOnDelegate += OnNoteOn;
    }

    void OnDestroy()
    {
        MidiMaster.noteOnDelegate -= OnNoteOn;
    }

    void HighlightNextKey(int index)
    {
        UnityEngine.Debug.Log("Highlighting MIDI Note: " + index);

        // Remove previous highlight
        if (activeKey != -1)
        {
            keyLayout.HighlightKey(activeKey, Color.white);
        }

        activeKey = index;
        keyLayout.HighlightKey(activeKey, keyHighlightColor);
        keyHighlightTime = Time.time;

        UpdateArrowPosition();
    }

    void UpdateArrowPosition()
    {
        if (arrow == null || activeKey == -1) return;

        GameObject keyObject = keyLayout.GetKeyObject(activeKey);
        if (keyObject != null)
        {
            arrow.transform.position = keyObject.transform.position + new Vector3(0, 0.05f, 0); // Adjust height
            arrow.transform.LookAt(keyObject.transform.position + new Vector3(0, 0, 1)); // Point forward

            // Calculate direction for rotation
            int nextKeyIndex = currentKeyIndex + 1;
            GameObject nextKeyObject = keyLayout.GetKeyObject(nextKeyIndex);
        
            if (nextKeyObject != null)
            {
                // Determine if the next key is to the right or left
                float direction = nextKeyObject.transform.position.x - keyObject.transform.position.x;
                
                // Set rotation based on direction - either 0 or 180 degrees around Y axis
                // This assumes the arrow points rightward at 0 degrees
                if (direction < 0)
                {
                    // Next key is to the left, rotate 180 degrees
                    arrow.transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    // Next key is to the right, keep at 0 degrees
                    arrow.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }
        }
    }

    void OnNoteOn(MidiChannel channel, int note, float velocity)
    {
        float keyPressTime = Time.time;
        float reactionTime = keyPressTime - keyHighlightTime;
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string accuracy;

        if (note == activeKey)
        {
            UnityEngine.Debug.Log("Correct Key Pressed: " + note);
            keyLayout.HighlightKey(note, Color.white); // Remove highlight
            accuracy = "Correct";

            // Move to the next key
            currentKeyIndex++;
            HighlightNextKey(currentKeyIndex);
        }
        else
        {
            UnityEngine.Debug.Log("Wrong Key Pressed: " + note);
            accuracy = "Incorrect";
        }

        // Log in CSV whether the key press was correct or incorrect
        WriteToCSV($"{note},{keyHighlightTime},{keyPressTime},{reactionTime},{accuracy},{timestamp}");
    }

    void WriteToCSV(string data, bool isHeader = false)
    {
        using (StreamWriter writer = new StreamWriter(csvFilePath, true))
        {
            writer.WriteLine(data);
        }
    }

    void OpenCSVFile()
    {
#if UNITY_EDITOR
        Process.Start(csvFilePath);
#endif
    }
}