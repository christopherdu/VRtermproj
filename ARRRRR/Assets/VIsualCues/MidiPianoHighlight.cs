using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MidiJack;

public class MIDIPianoGame : MonoBehaviour
{
    [Header("References")]
    public CasiotoneKeyLayout keyLayout;

    [Header("Highlight Colors")]
    public Color firstKeyHighlight = Color.red;
    public Color secondKeyHighlight = Color.yellow;
    public Color thirdKeyHighlight = Color.green;

    private int currentKeyIndex = 36; // Start at MIDI note 36 (C2)
    private int keysPerSet = 3;
    private List<int> activeKeys = new List<int>();

    void Start()
    {
        StartCoroutine(WaitForKeysAndHighlight());
    }

    IEnumerator WaitForKeysAndHighlight()
    {
        // Wait until the keyboard has instantiated all keys
        while (keyLayout.GetKeyObject(currentKeyIndex) == null)
        {
            Debug.Log("Waiting for keyboard to initialize...");
            yield return null; // Waits one frame before checking again
        }

        Debug.Log("Keyboard initialized, highlighting first set.");
        HighlightNextSet(currentKeyIndex);
        MidiMaster.noteOnDelegate += OnNoteOn;
    }

    void OnDestroy()
    {
        MidiMaster.noteOnDelegate -= OnNoteOn;
    }

    void HighlightNextSet(int index)
    {
        Debug.Log("Highlighting New Set Starting at MIDI Note: " + index);
        activeKeys.Clear();

        for (int i = 0; i < keysPerSet; i++)
        {
            int note = index + i;
            Debug.Log("Trying to Highlight Note: " + note);
            activeKeys.Add(note);

            if (i == 0)
                keyLayout.HighlightKey(note, firstKeyHighlight);
            else if (i == 1)
                keyLayout.HighlightKey(note, secondKeyHighlight);
            else if (i == 2)
                keyLayout.HighlightKey(note, thirdKeyHighlight);
        }
    }

    void OnNoteOn(MidiChannel channel, int note, float velocity)
    {
        if (activeKeys.Contains(note))
        {
            Debug.Log("Correct Key Pressed: " + note);
            keyLayout.HighlightKey(note, Color.white); // Remove highlight when pressed
            activeKeys.Remove(note);
        }
        else
        {
            Debug.Log("Wrong Key Pressed: " + note);
        }

        if (activeKeys.Count == 0) // Move to the next set immediately
        {
            currentKeyIndex += keysPerSet;
            HighlightNextSet(currentKeyIndex);
        }
    }
}