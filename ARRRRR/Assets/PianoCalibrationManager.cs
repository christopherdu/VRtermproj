using UnityEngine;
using System.Collections.Generic;

public class PianoMIDICalibrationManager : MonoBehaviour
{
    [Header("References")]
    public Transform pianoTransform;  // The transform of your virtual piano
    public CasiotoneKeyLayout keyLayout;  // Reference to your key layout script
    
    [Header("Calibration Settings")]
    public int[] calibrationNotes = new int[] { 36, 60, 84 };  // Low C, Middle C, High C
    public Color highlightColor = Color.red;
    
    [Header("Adjustment Parameters")]
    public float positionAdjustmentFactor = 0.5f;  // How quickly to adjust position
    public float rotationAdjustmentFactor = 5.0f;  // How quickly to adjust rotation
    
    [Header("State")]
    public bool isCalibrating = false;
    public int currentCalibrationStep = 0;
    
    // List to track detected calibration notes
    private List<Vector3> keyPositions = new List<Vector3>();
    private List<int> detectedNotes = new List<int>();
    
    // Original position before calibration
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    public void StartCalibration()
    {
        if (pianoTransform == null || keyLayout == null)
        {
            Debug.LogError("Missing references for calibration");
            return;
        }
        
        // Store original position in case we need to revert
        originalPosition = pianoTransform.position;
        originalRotation = pianoTransform.rotation;
        
        // Reset calibration state
        isCalibrating = true;
        currentCalibrationStep = 0;
        keyPositions.Clear();
        detectedNotes.Clear();
        
        // Highlight the first key
        HighlightCalibrationKey(calibrationNotes[0]);
        
        Debug.Log("Piano calibration started. Press the highlighted key on your Casiotone CT-S300.");
    }
    
    public void ProcessMIDINote(int midiNote)
    {
        if (!isCalibrating) return;
        
        Debug.Log($"Calibration received MIDI note: {midiNote}");
        
        // Check if this is close to our expected note (allow some tolerance)
        int expectedNote = calibrationNotes[currentCalibrationStep];
        if (Mathf.Abs(midiNote - expectedNote) <= 2)  // Allow for small errors
        {
            // Store the actual detected note
            detectedNotes.Add(midiNote);
            
            // Get the virtual key position
            GameObject keyObj = keyLayout.GetKeyObject(expectedNote);
            if (keyObj != null)
            {
                // Store world position of this key
                keyPositions.Add(keyObj.transform.position);
                
                // Process this calibration point
                ProcessCalibrationPoint(keyPositions.Count - 1);
                
                // Move to next step
                currentCalibrationStep++;
                
                // Check if we're done or should move to next key
                if (currentCalibrationStep >= calibrationNotes.Length)
                {
                    FinishCalibration();
                }
                else
                {
                    // Highlight next key
                    HighlightCalibrationKey(calibrationNotes[currentCalibrationStep]);
                }
            }
        }
    }
    
    private void HighlightCalibrationKey(int midiNote)
    {
        // Reset all keys to default first
        keyLayout.ResetAllKeys();
        
        // Highlight the specific key
        GameObject keyObj = keyLayout.GetKeyObject(midiNote);
        if (keyObj != null)
        {
            Renderer renderer = keyObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = highlightColor;
            }
        }
    }
    
    private void ProcessCalibrationPoint(int pointIndex)
    {
        // Different adjustments based on which calibration point we're processing
        switch (pointIndex)
        {
            case 0:  // First key (usually leftmost)
                // Initial position adjustment
                InitialPositionAdjustment();
                break;
                
            case 1:  // Second key (usually middle)
                // Refine rotation
                AdjustRotation();
                break;
                
            case 2:  // Third key (usually rightmost)
                // Fine-tune position
                FinalPositionAdjustment();
                break;
        }
    }
    
    private void InitialPositionAdjustment()
    {
        // For first key, we mainly adjust horizontal position
        // This assumes the first key is on the left side of keyboard
        
        // Get the virtual key position from our list
        Vector3 keyPosition = keyPositions[0];
        
        // Adjust the piano position to align with this key
        // We're estimating where the physical key might be relative to the controller
        Vector3 adjustment = Vector3.zero;
        
        // Move piano so this key aligns with where we believe the physical key is
        // This is a simplification - you may need to refine this based on testing
        adjustment.x = -0.2f; // Move slightly left
        
        // Apply the adjustment, scaled by our adjustment factor
        pianoTransform.position += adjustment * positionAdjustmentFactor;
    }
    
    private void AdjustRotation()
    {
        // With two points, we can determine rotation
        if (keyPositions.Count >= 2)
        {
            // Get positions of first and second key
            Vector3 firstKey = keyPositions[0];
            Vector3 secondKey = keyPositions[1];
            
            // Calculate the expected angle between these keys
            Vector3 keyDirection = secondKey - firstKey;
            
            // Calculate the target rotation based on key direction
            // This is a simplified version - you may need to refine this
            float angleAdjustment = Mathf.Atan2(keyDirection.z, keyDirection.x) * Mathf.Rad2Deg;
            angleAdjustment = angleAdjustment - 90; // Adjust as needed for your setup
            
            // Apply a partial rotation adjustment
            pianoTransform.Rotate(0, angleAdjustment * rotationAdjustmentFactor * Time.deltaTime, 0);
        }
    }
    
    private void FinalPositionAdjustment()
    {
        // With three points, we can fine-tune position
        if (keyPositions.Count >= 3)
        {
            // Calculate a centroid of our three key positions
            Vector3 centroid = Vector3.zero;
            foreach (Vector3 pos in keyPositions)
            {
                centroid += pos;
            }
            centroid /= keyPositions.Count;
            
            // Adjust the piano to center around this point
            Vector3 offset = centroid - pianoTransform.position;
            offset.y = 0; // Maintain vertical position
            
            // Apply a small adjustment toward the centroid
            pianoTransform.position += offset * positionAdjustmentFactor * 0.5f;
        }
    }
    
    private void FinishCalibration()
    {
        Debug.Log("Piano calibration complete!");
        
        // Reset all keys to default
        keyLayout.ResetAllKeys();
        
        // Exit calibration mode
        isCalibrating = false;
        
        // Save the calibrated position
        SaveCalibratedPosition();
    }
    
    private void SaveCalibratedPosition()
    {
        // Save the position and rotation to PlayerPrefs
        PlayerPrefs.SetFloat("PianoCalibPosX", pianoTransform.position.x);
        PlayerPrefs.SetFloat("PianoCalibPosY", pianoTransform.position.y);
        PlayerPrefs.SetFloat("PianoCalibPosZ", pianoTransform.position.z);
        
        PlayerPrefs.SetFloat("PianoCalibRotX", pianoTransform.rotation.eulerAngles.x);
        PlayerPrefs.SetFloat("PianoCalibRotY", pianoTransform.rotation.eulerAngles.y);
        PlayerPrefs.SetFloat("PianoCalibRotZ", pianoTransform.rotation.eulerAngles.z);
        
        PlayerPrefs.Save();
    }
    
    public void LoadCalibratedPosition()
    {
        // Check if we have saved calibration data
        if (PlayerPrefs.HasKey("PianoCalibPosX"))
        {
            // Load position
            Vector3 savedPos = new Vector3(
                PlayerPrefs.GetFloat("PianoCalibPosX"),
                PlayerPrefs.GetFloat("PianoCalibPosY"),
                PlayerPrefs.GetFloat("PianoCalibPosZ")
            );
            
            // Load rotation
            Vector3 savedRot = new Vector3(
                PlayerPrefs.GetFloat("PianoCalibRotX"),
                PlayerPrefs.GetFloat("PianoCalibRotY"),
                PlayerPrefs.GetFloat("PianoCalibRotZ")
            );
            
            // Apply saved transformation
            pianoTransform.position = savedPos;
            pianoTransform.rotation = Quaternion.Euler(savedRot);
            
            Debug.Log("Loaded calibrated piano position");
        }
    }
    
    public void CancelCalibration()
    {
        if (!isCalibrating) return;
        
        // Restore original position
        pianoTransform.position = originalPosition;
        pianoTransform.rotation = originalRotation;
        
        // Reset all keys
        keyLayout.ResetAllKeys();
        
        isCalibrating = false;
        
        Debug.Log("Calibration cancelled");
    }
}