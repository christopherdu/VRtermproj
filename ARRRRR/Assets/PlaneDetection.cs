using UnityEngine;

public class SimplePianoDetector : MonoBehaviour
{
    [Header("Casiotone CT-S300 Dimensions")]
    public float pianoWidth = 0.93f;  // 930mm in meters
    public float pianoDepth = 0.256f; // 256mm in meters
    public float pianoHeight = 0.073f; // 73mm in meters
    
    [Header("References")]
    public Transform pianoTransform; // Set this manually in the Unity Inspector
    public CasiotoneKeyLayout keyLayout;
    
    // For manual positioning
    public bool useManualPlacement = true;
    private bool isPlacing = false;
    
    void Start()
    {
        // If we're using manual placement, start in placement mode
        if (useManualPlacement)
        {
            StartManualPlacement();
        }
    }
    
    void Update()
    {
        if (isPlacing)
        {
            // For testing in editor, you can use mouse position for placement
            // In VR, you'd use controller position instead
            if (Input.GetMouseButtonDown(0)) 
            {
                ConfirmPlacement();
            }
            
            // Move the piano with the controller/mouse
            UpdatePlacementPosition();
        }
    }
    
    public void StartManualPlacement()
    {
        isPlacing = true;
        
        // Create a temporary piano visual if needed
        if (pianoTransform == null)
        {
            GameObject tempPiano = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempPiano.name = "TempPianoVisual";
            tempPiano.transform.localScale = new Vector3(pianoWidth, pianoHeight, pianoDepth);
            
            // Make it semi-transparent
            Renderer renderer = tempPiano.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.2f, 0.6f, 1.0f, 0.5f);
            renderer.material = material;
            
            pianoTransform = tempPiano.transform;
        }
        
        Debug.Log("Started manual piano placement. Position the piano and click to confirm.");
    }
    
    private void UpdatePlacementPosition()
    {
        // For editor testing - in real VR, use controller position
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X") * 0.1f;
            float mouseY = Input.GetAxis("Mouse Y") * 0.1f;
            
            // Move piano based on mouse
            pianoTransform.Translate(mouseX, 0, mouseY, Space.World);
            
            // Allow rotation with keys
            if (Input.GetKey(KeyCode.Q))
                pianoTransform.Rotate(0, -1, 0);
            if (Input.GetKey(KeyCode.E))
                pianoTransform.Rotate(0, 1, 0);
        }
    }
    
    private void ConfirmPlacement()
    {
        isPlacing = false;
        Debug.Log("Piano placement confirmed!");
        
        // Initialize the key layout
        if (keyLayout != null)
        {
            keyLayout.CreateKeyboardLayout();
        }
    }
}