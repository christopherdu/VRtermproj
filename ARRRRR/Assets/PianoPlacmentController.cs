using UnityEngine;
using System.Collections;
using TMPro;

public class PianoPlacementController : MonoBehaviour
{
    [Header("Piano References")]
    public Transform pianoModel;  // The 3D model of your piano
    public Transform keysParent;  // Parent transform for all the piano keys
    
    [Header("Piano Dimensions")]
    public float pianoWidth = 0.93f;   // 930mm (Casiotone CT-S300)
    public float pianoDepth = 0.256f;  // 256mm
    public float pianoHeight = 0.073f; // 73mm
    
    [Header("Placement Settings")]
    public float verticalOffset = 0.01f;    // Initial vertical offset
    public float rotationSpeed = 30f;  // Degrees per second
    public float heightAdjustSpeed = 0.05f; // Meters per second
    public Color placementColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color confirmedColor = new Color(0.2f, 0.2f, 0.8f, 0.8f);
    public float confirmationDisplayTime = 2.0f;  // Time to show confirmation color before hiding
    
    [Header("Alignment Markers")]
    public GameObject leftMarker;
    public GameObject rightMarker;
    public GameObject heightGuide; // Visual guide showing current height
    
    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI heightValueText;
    
    [Header("State")]
    public bool isPlacementMode = true;
    public enum AlignmentStep { PositionLeft, PositionRight, AdjustHeight, AdjustRotation }
    public AlignmentStep currentStep = AlignmentStep.PositionLeft;
    
    // Key positions
    private Vector3 leftKeyPosition;
    private Vector3 rightKeyPosition;
    
    // Reference to the piano renderer
    private Renderer pianoRenderer;
    // For tracking button states
    private bool wasConfirmButtonPressed = false;
    private bool wasRepositionButtonPressed = false;
    
    void Start()
    {
        // Get renderer component
        if (pianoModel != null)
        {
            pianoRenderer = pianoModel.GetComponent<Renderer>();
            
            if (pianoRenderer != null)
            {
                // Set to placement color
                pianoRenderer.material.color = placementColor;
            }
        }
        
        // Initialize markers
        if (leftMarker) leftMarker.SetActive(true);
        if (rightMarker) rightMarker.SetActive(false);
        if (heightGuide) heightGuide.SetActive(false);
        
        // Set initial text prompt
        UpdateInstructionText("Position left marker at the leftmost piano key, then press trigger");
    }
    
    void Update()
    {
        // Check for repositioning request (Y button)
        CheckForRepositionRequest();
        
        if (isPlacementMode)
        {
            // Update alignment based on current step
            switch (currentStep)
            {
                case AlignmentStep.PositionLeft:
                    UpdateLeftMarkerPosition();
                    break;
                case AlignmentStep.PositionRight:
                    UpdateRightMarkerPosition();
                    break;
                case AlignmentStep.AdjustHeight:
                    UpdateHeightAdjustment();
                    break;
                case AlignmentStep.AdjustRotation:
                    UpdatePianoRotationAndHeight();
                    break;
            }
            
            // Check for step confirmation
            CheckForStepConfirmation();
        }
    }
    
    void UpdateLeftMarkerPosition()
    {
        if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch))
        {
            // Get controller position
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            
            // Convert to world space
            Transform trackingSpace = FindTrackingSpace();
            if (trackingSpace != null)
            {
                controllerPosition = trackingSpace.TransformPoint(controllerPosition);
            }
            
            // Update left marker position
            if (leftMarker) leftMarker.transform.position = controllerPosition;
        }
    }
    
    void UpdateRightMarkerPosition()
    {
        if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch))
        {
            // Get controller position
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            
            // Convert to world space
            Transform trackingSpace = FindTrackingSpace();
            if (trackingSpace != null)
            {
                controllerPosition = trackingSpace.TransformPoint(controllerPosition);
            }
            
            // Update right marker position
            if (rightMarker) rightMarker.transform.position = controllerPosition;
            
            // Update piano position and width based on both markers
            if (leftMarker && pianoModel)
            {
                // Calculate center position between markers
                Vector3 center = Vector3.Lerp(leftMarker.transform.position, rightMarker.transform.position, 0.5f);
                center.y += verticalOffset; // Add vertical offset
                
                // Set piano position to center
                pianoModel.position = center;
                
                // Calculate the forward direction - facing the user
                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 direction = new Vector3(cameraPosition.x - center.x, 0, cameraPosition.z - center.z).normalized;
                pianoModel.rotation = Quaternion.LookRotation(direction);
                
                // Scale piano width to match marker distance
                float markerDistance = Vector3.Distance(
                    new Vector3(leftMarker.transform.position.x, 0, leftMarker.transform.position.z),
                    new Vector3(rightMarker.transform.position.x, 0, rightMarker.transform.position.z)
                );
                
                // Calculate scale factor
                float scaleX = markerDistance / pianoWidth;
                pianoModel.localScale = new Vector3(scaleX, pianoModel.localScale.y, pianoModel.localScale.z);
            }
        }
    }
    
    void UpdateHeightAdjustment()
    {
        // Show height guide
        if (heightGuide)
        {
            heightGuide.SetActive(true);
            
            // Update height guide position at piano's current height
            heightGuide.transform.position = new Vector3(
                pianoModel.position.x,
                pianoModel.position.y,
                pianoModel.position.z
            );
            
            // Scale the height guide to match the piano dimensions
            heightGuide.transform.localScale = new Vector3(
                pianoWidth * pianoModel.localScale.x,
                0.001f, // Thin plane
                pianoDepth
            );
            
            // Make sure it's correctly oriented
            heightGuide.transform.rotation = pianoModel.rotation;
        }
        
        // Use thumbsticks to control height
        float leftThumbstickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
        float rightThumbstickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
        float combinedY = (leftThumbstickY + rightThumbstickY) * 0.5f;
        
        if (Mathf.Abs(combinedY) > 0.1f)
        {
            // Adjust height at a controlled rate
            float heightAdjustment = combinedY * heightAdjustSpeed * Time.deltaTime;
            Vector3 position = pianoModel.position;
            position.y += heightAdjustment;
            pianoModel.position = position;
            
            // Update height display
            UpdateHeightValueText(position.y);
        }
        
        // Direct height setting with hand trigger (grip)
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            // Get controller position
            Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            
            // Convert to world space
            Transform trackingSpace = FindTrackingSpace();
            if (trackingSpace != null)
            {
                controllerPos = trackingSpace.TransformPoint(controllerPos);
            }
            
            // Set piano height directly to controller height
            Vector3 pianoPos = pianoModel.position;
            pianoPos.y = controllerPos.y;
            pianoModel.position = pianoPos;
            
            // Update height display
            UpdateHeightValueText(pianoPos.y);
            
            // Provide haptic feedback
            OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.LTouch);
        }
        
        // Measure height with raycast (for physical surface detection)
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)) // A button
        {
            MeasureHeightWithRayCast();
        }
    }
    
    public void MeasureHeightWithRayCast()
    {
        // Get controller position
        Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Transform trackingSpace = FindTrackingSpace();
        if (trackingSpace != null)
        {
            controllerPos = trackingSpace.TransformPoint(controllerPos);
        }
        
        // Cast ray downward
        RaycastHit hit;
        if (Physics.Raycast(controllerPos, Vector3.down, out hit, 0.5f))
        {
            // Detected a surface below, use this as the piano height
            Vector3 pianoPos = pianoModel.position;
            pianoPos.y = hit.point.y + 0.01f; // Add 1cm offset
            pianoModel.position = pianoPos;
            
            // Update height display
            UpdateHeightValueText(pianoPos.y);
            
            // Provide feedback
            OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
            UpdateInstructionText("Piano height set at detected surface");
        }
    }
    
    void UpdatePianoRotationAndHeight()
    {
        // Hide height guide if still visible
        if (heightGuide) heightGuide.SetActive(false);
        
        // Rotation control
        float leftThumbstickX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).x;
        float rightThumbstickX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
        float rotationAmount = (leftThumbstickX + rightThumbstickX) * rotationSpeed * Time.deltaTime;
        
        if (Mathf.Abs(rotationAmount) > 0.01f)
        {
            pianoModel.Rotate(0, rotationAmount, 0);
        }
        
        // Continue allowing height adjustments
        float leftThumbstickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
        float rightThumbstickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
        float heightAmount = (leftThumbstickY + rightThumbstickY) * 0.5f * heightAdjustSpeed * Time.deltaTime;
        
        if (Mathf.Abs(heightAmount) > 0.0001f)
        {
            Vector3 position = pianoModel.position;
            position.y += heightAmount;
            pianoModel.position = position;
            
            // Update height display
            UpdateHeightValueText(position.y);
        }
    }
    
    void CheckForStepConfirmation()
    {
        // Determine which controller to use for confirmation
        OVRInput.Controller confirmController;
        
        if (currentStep == AlignmentStep.PositionLeft)
        {
            confirmController = OVRInput.Controller.LTouch;
        }
        else if (currentStep == AlignmentStep.PositionRight)
        {
            confirmController = OVRInput.Controller.RTouch;
        }
        else
        {
            // For height and rotation steps, either controller can confirm
            bool isLeftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            bool isRightTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            
            if ((isLeftTriggerPressed || isRightTriggerPressed) && !wasConfirmButtonPressed)
            {
                AdvanceAlignmentStep();
                wasConfirmButtonPressed = true;
                return;
            }
            
            wasConfirmButtonPressed = isLeftTriggerPressed || isRightTriggerPressed;
            return;
        }
        
        // Check if trigger is pressed
        bool isConfirmPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, confirmController);
        
        // Detect button press (not held)
        if (isConfirmPressed && !wasConfirmButtonPressed)
        {
            AdvanceAlignmentStep();
        }
        
        wasConfirmButtonPressed = isConfirmPressed;
    }
    
    void AdvanceAlignmentStep()
    {
        switch (currentStep)
        {
            case AlignmentStep.PositionLeft:
                // Store left marker position
                leftKeyPosition = leftMarker.transform.position;
                
                // Activate right marker
                if (rightMarker) rightMarker.SetActive(true);
                
                // Update state
                currentStep = AlignmentStep.PositionRight;
                UpdateInstructionText("Position right marker at the rightmost piano key, then press trigger");
                
                break;
                
            case AlignmentStep.PositionRight:
                // Store right marker position
                rightKeyPosition = rightMarker.transform.position;
                
                // Update state
                currentStep = AlignmentStep.AdjustHeight;
                UpdateInstructionText("Adjust piano height:\n• Use thumbsticks to move up/down\n• Use grip to set to controller height\n• Press A to detect surface below\n• Press trigger when satisfied");
                
                break;
                
            case AlignmentStep.AdjustHeight:
                // Hide height adjustment visuals
                if (heightGuide) heightGuide.SetActive(false);
                
                // Update state
                currentStep = AlignmentStep.AdjustRotation;
                UpdateInstructionText("Fine-tune rotation with thumbsticks left/right.\nHeight can still be adjusted with thumbsticks up/down.\nPress trigger to confirm placement.");
                
                break;
                
            case AlignmentStep.AdjustRotation:
                ConfirmPlacement();
                break;
        }
        
        // Provide haptic feedback
        OVRInput.SetControllerVibration(0.3f, 0.3f, OVRInput.Controller.LTouch);
        if (currentStep != AlignmentStep.PositionLeft)
        {
            OVRInput.SetControllerVibration(0.3f, 0.3f, OVRInput.Controller.RTouch);
        }
    }
    
    void CheckForRepositionRequest()
    {
        // Check if Y button is pressed (Button.Two on left controller is Y)
        bool isRepositionPressed = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
        
        // Detect button press (not held)
        if (isRepositionPressed && !wasRepositionButtonPressed)
        {
            RestartPlacement();
        }
        
        wasRepositionButtonPressed = isRepositionPressed;
    }
    
    void ConfirmPlacement()
    {
        Debug.Log("Piano placement confirmed!");
        
        // Change color to indicate placement is confirmed
        if (pianoRenderer != null)
        {
            pianoRenderer.material.color = confirmedColor;
            
            // Start the coroutine to hide the piano model after delay
            StartCoroutine(HidePianoModelAfterDelay());
        }
        
        // Hide markers
        if (leftMarker) leftMarker.SetActive(false);
        if (rightMarker) rightMarker.SetActive(false);
        if (heightGuide) heightGuide.SetActive(false);
        
        // Exit placement mode
        isPlacementMode = false;
        
        // Clear instruction text
        UpdateInstructionText("Piano placement complete");
        UpdateHeightValueText(0, false); // Hide height value
        
        // Provide haptic feedback
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
        
        // Initialize piano keys
        InitializePianoKeys();
    }
    
    IEnumerator HidePianoModelAfterDelay()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(confirmationDisplayTime);
        
        // Hide the piano model renderer
        if (pianoRenderer != null)
        {
            pianoRenderer.enabled = false;
        }
    }
    
    void RestartPlacement()
    {
        Debug.Log("Restarting piano placement...");
        
        // Make sure the renderer is enabled
        if (pianoRenderer != null)
        {
            pianoRenderer.enabled = true;
            pianoRenderer.material.color = placementColor;
        }
        
        // Clear existing keys
        if (keysParent != null)
        {
            foreach (Transform child in keysParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Reset markers
        if (leftMarker) leftMarker.SetActive(true);
        if (rightMarker) rightMarker.SetActive(false);
        if (heightGuide) heightGuide.SetActive(false);
        
        // Reset state
        isPlacementMode = true;
        currentStep = AlignmentStep.PositionLeft;
        
        // Update instruction text
        UpdateInstructionText("Position left marker at the leftmost piano key, then press trigger");
        UpdateHeightValueText(0, false); // Hide height value
        
        // Provide haptic feedback to confirm the action
        OVRInput.SetControllerVibration(0.3f, 0.3f, OVRInput.Controller.LTouch);
    }
    
    void InitializePianoKeys()
    {
        // Call your key layout script
        CasiotoneKeyLayout keyLayout = GetComponent<CasiotoneKeyLayout>();
        if (keyLayout != null)
        {
            keyLayout.CreateKeyboardLayout();
        }
    }
    
    // Helper to find tracking space
    Transform FindTrackingSpace()
    {
        OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig != null)
        {
            return cameraRig.trackingSpace;
        }
        return Camera.main?.transform.parent;
    }
    
    // Helper to update UI text instruction
    void UpdateInstructionText(string message)
    {
        if (instructionText != null)
        {
            instructionText.text = message;
        }
        Debug.Log("Instruction: " + message);
    }
    
    // Helper to update height value display
    void UpdateHeightValueText(float height, bool visible = true)
    {
        if (heightValueText != null)
        {
            if (visible)
            {
                heightValueText.gameObject.SetActive(true);
                heightValueText.text = $"Height: {height:F3}m";
            }
            else
            {
                heightValueText.gameObject.SetActive(false);
            }
        }
    }
}