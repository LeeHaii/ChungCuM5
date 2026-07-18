using UnityEngine;
using Michsky.MUIP;
using UnityEngine.UI;

public class AppModeController : MonoBehaviour
{
    [SerializeField] private Button buttonUnitSearch;
    [SerializeField] private Button buttonBim;
    
    [SerializeField] private GameObject ApartmentInfoPanel;
    [SerializeField] private GameObject BimDataPanel;
    [SerializeField] private BimDataProperties bimDataProperties;

    [SerializeField] private GameObject floor;

    void Start()
    {
        // Try to auto-bind if not set
        if (buttonBim == null)
        {
            var go = GameObject.Find("ButtonBIM");
            if (go) buttonBim = go.GetComponent<Button>();
        }
        if (buttonUnitSearch == null)
        {
            var go = GameObject.Find("ButtonUnitSearch");
            if (go) buttonUnitSearch = go.GetComponent<Button>();
        }
        if (bimDataProperties == null) bimDataProperties = FindObjectOfType<BimDataProperties>(true);
        if (ApartmentInfoPanel == null) ApartmentInfoPanel = GameObject.Find("ApartmentInfoPanel");
        if (BimDataPanel == null) BimDataPanel = GameObject.Find("PanelBimINFO");

        ApartmentInfoPanel.SetActive(false);
        BimDataPanel.SetActive(false);
        floor.SetActive(false);

        if (buttonBim != null)
        {
            buttonBim.onClick.AddListener(SetBimMode);
        }
        else
        {
            Debug.LogWarning("[AppModeController] ButtonBIM not found.");
        }
        
        if (buttonUnitSearch != null)
        {
            buttonUnitSearch.onClick.AddListener(SetUnitSearchMode);
        }
        else
        {
            Debug.LogWarning("[AppModeController] ButtonUnitSearch not found.");
        }

        // Initialize to Unit Search mode by default
        //SetUnitSearchMode();
    }

    private void SetBimMode()
    {
        if (BimDataPanel) {
            BimDataPanel.SetActive(true); // Ensure the panel is active so Update runs
        }
        if (bimDataProperties) bimDataProperties.EnableBIMData();
        ExitUnitSearchMode();
        Debug.Log("[Mode] Switched to BIM Mode");
    }

    private void SetUnitSearchMode()
    {
        if (ApartmentInfoPanel) {
            ApartmentInfoPanel.SetActive(true); // Ensure it's active
            floor.SetActive(true);
        }
        ExitBimMode();
        Debug.Log("[Mode] Switched to Unit Search Mode");
    }

    private void ExitBimMode()
    {
        if (bimDataProperties) bimDataProperties.DisableBIMData();
        if(BimDataPanel) BimDataPanel.SetActive(false);
    }

    private void ExitUnitSearchMode()
    {
        if (ApartmentInfoPanel) ApartmentInfoPanel.SetActive(false);
        if(floor) floor.SetActive(false);
    }
}
