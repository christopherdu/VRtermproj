using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationUI : MonoBehaviour
{
    [Header("References")]
    public PianoMIDICalibrationManager calibrationManager;
    
    [Header("UI Elements")]
    public Button startCalibrationButton;
    public Button cancelCalibrationButton;
    public Button resetCalibrationButton;
    public TextMeshProUGUI statusText;
    public GameObject calibrationPanel;
    
    [Header("Instruction Images")]
    public GameObject[] calibrationStepImages;
    
    void Start()
    {
        // Set up button actions
        startCalibrationButton.onClick.AddListener(StartCalibration);
        cancelCalibrationButton.onClick.AddListener(CancelCalibration);
        resetCalibrationButton.onClick.AddListener(ResetCalibration);
        
        // Initially hide calibration panel
        calibrationPanel.SetActive(false);
        
        // Hide all instruction images
        foreach (GameObject img in calibrationStepImages)
        {
            img.SetActive(false);
        }
        
        // Load calibrated position if available
        if (calibrationManager != null)
        {
            calibrationManager.LoadCalibratedPosition();
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        // Update UI based on calibration state
        if (calibrationManager != null && calibrationManager.isCalibrating)
        {
            UpdateCalibrationUI();
        }
    }
    
    private void UpdateCalibrationUI()
    {
        // Show appropriate step image
        for (int i = 0; i < calibrationStepImages.Length; i++)
        {
            calibrationStepImages[i].SetActive(i == calibrationManager.currentCalibrationStep);
        }
        
        // Update status text
        int step = calibrationManager.currentCalibrationStep + 1;
        int total = calibrationManager.calibrationNotes.Length;
        statusText.text = $"Step {step} of {total}: Press the highlighted key";
    }
    
    void StartCalibration()
    {
        if (calibrationManager != null)
        {
            calibrationManager.StartCalibration();
            calibrationPanel.SetActive(true);
            startCalibrationButton.gameObject.SetActive(false);
            cancelCalibrationButton.gameObject.SetActive(true);
        }
    }
    
    void CancelCalibration()
    {
        if (calibrationManager != null)
        {
            calibrationManager.CancelCalibration();
            calibrationPanel.SetActive(false);
            startCalibrationButton.gameObject.SetActive(true);
            cancelCalibrationButton.gameObject.SetActive(false);
        }
    }
    
    void ResetCalibration()
    {
        // Clear saved calibration data
        PlayerPrefs.DeleteKey("PianoCalibPosX");
        PlayerPrefs.DeleteKey("PianoCalibPosY");
        PlayerPrefs.DeleteKey("PianoCalibPosZ");
        PlayerPrefs.DeleteKey("PianoCalibRotX");
        PlayerPrefs.DeleteKey("PianoCalibRotY");
        PlayerPrefs.DeleteKey("PianoCalibRotZ");
        PlayerPrefs.Save();
        
        // Update UI
        statusText.text = "Calibration data reset";
    }
    
    void UpdateUI()
    {
        bool hasCalibrationData = PlayerPrefs.HasKey("PianoCalibPosX");
        resetCalibrationButton.interactable = hasCalibrationData;
    }
}