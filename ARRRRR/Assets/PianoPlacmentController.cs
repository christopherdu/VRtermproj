using UnityEngine;
using System.Collections;

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
    public float sideOffset = 0.5f;    // Distance to the right of controller
    public float rotationSpeed = 30f;  // Degrees per second
    public Color placementColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color confirmedColor = new Color(0.2f, 0.2f, 0.8f, 0.8f);
    public float confirmationDisplayTime = 2.0f;  // Time to show confirmation color before hiding
    
    [Header("State")]
    public bool isPlacementMode = true;
    
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
    }
    
    void Update()
    {
        // Check for repositioning request (Y button)
        CheckForRepositionRequest();
        
        if (isPlacementMode)
        {
            // Update piano position based on left controller
            UpdatePianoPosition();
            
            // Check for placement confirmation
            CheckForPlacementConfirmation();
        }
    }
    
    void UpdatePianoPosition()
    {
        // Check if left controller is tracked
        if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch))
        {
            // Get controller position and rotation
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            
            // Convert to world space
            Transform trackingSpace = FindTrackingSpace();
            if (trackingSpace != null)
            {
                controllerPosition = trackingSpace.TransformPoint(controllerPosition);
                controllerRotation = trackingSpace.rotation * controllerRotation;
            }
            
            // Calculate the right vector from the controller
            Vector3 rightVector = controllerRotation * Vector3.right;
            
            // Position piano to the right of the controller
            Vector3 pianoPosition = controllerPosition + (rightVector * sideOffset);
            
            // Set position
            pianoModel.position = pianoPosition;
            
            // For rotation, use Y-axis rotation to keep piano level
            // Then add 180 degrees to make keys face the user
            Vector3 eulerAngles = controllerRotation.eulerAngles;
            pianoModel.rotation = Quaternion.Euler(0, eulerAngles.y + 180f, 0);
            
            // Allow fine rotation using thumbstick
            float thumbstickX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).x;
            if (Mathf.Abs(thumbstickX) > 0.1f)
            {
                pianoModel.Rotate(0, thumbstickX * rotationSpeed * Time.deltaTime, 0);
            }
        }
    }
    
    void CheckForPlacementConfirmation()
    {
        // Check if trigger is pressed
        bool isConfirmPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        
        // Detect button press (not held)
        if (isConfirmPressed && !wasConfirmButtonPressed)
        {
            ConfirmPlacement();
        }
        
        wasConfirmButtonPressed = isConfirmPressed;
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
        
        // Exit placement mode
        isPlacementMode = false;
        
        // Provide haptic feedback
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
        
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
        
        // Enter placement mode
        isPlacementMode = true;
        
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
}