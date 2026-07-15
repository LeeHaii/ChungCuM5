using UnityEngine;
using Michsky.MUIP;

public class AppModeController : MonoBehaviour
{
    public ButtonManager buttonBim;
    public ButtonManager buttonUnitSearch;

    public GameObject ApartmentInfoPanel;
    public GameObject BimDataPanel;
    public BimDataProperties bimDataProperties;

    [SerializeField] private GameObject floor;

    void Start()
    {
        // Try to auto-bind if not set
        if (buttonBim == null)
        {
            var go = GameObject.Find("ButtonBIM");
            if (go) buttonBim = go.GetComponent<ButtonManager>();
        }
        if (buttonUnitSearch == null)
        {
            var go = GameObject.Find("ButtonUnitSearch");
            if (go) buttonUnitSearch = go.GetComponent<ButtonManager>();
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

    public void SetBimMode()
    {
        if (BimDataPanel) {
            BimDataPanel.SetActive(true); // Ensure the panel is active so Update runs
        }
        if (floor) floor.SetActive(false);
        if (bimDataProperties) bimDataProperties.EnableBIMData();
        Debug.Log("[Mode] Switched to BIM Mode");
    }

    public void SetUnitSearchMode()
    {
        if (ApartmentInfoPanel) {
            ApartmentInfoPanel.SetActive(true); // Ensure it's active
            floor.SetActive(true);
        }
        if (bimDataProperties) bimDataProperties.DisableBIMData();
        Debug.Log("[Mode] Switched to Unit Search Mode");
    }
}
