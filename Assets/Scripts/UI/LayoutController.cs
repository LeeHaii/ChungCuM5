using UnityEngine;
using UnityEngine.UI;

public class LayoutController : MonoBehaviour
{
    [Header("Main UI RectTransforms")]
    [SerializeField] private RectTransform leftPanel;
    [SerializeField] private RectTransform viewportPanel;
    [SerializeField] private RectTransform upperPanel;

    [Header("Content Panels inside Left Panel")]
    [SerializeField] private GameObject panelBimInfo; // PanelBImINFO
    [SerializeField] private GameObject panelCuDan;   // PanelCuDan
    
    [Header("Buttons")]
    [SerializeField] private Button buttonBim;
    [SerializeField] private Button buttonUnitSearch;

    private void Start()
    {
        panelBimInfo.SetActive(false);
        panelCuDan.SetActive(false);
        // Start in the default thin state
        buttonBim.onClick.AddListener(ShowBimState);
        buttonUnitSearch.onClick.AddListener(ShowUnitSearchState);
        ShowDefaultState();
    }

    // --- BUTTON TRIGGER FUNCTIONS ---

    public void ShowDefaultState()
    {
        UpdateLayout(
            leftWidth: 35f, 
            upperLeftOffset: 35f, 
            viewportLeftOffset: 35f, 
            showBim: false, 
            showCuDan: false
        );
    }

    public void ShowBimState()
    {
        UpdateLayout(
            leftWidth: 190f, 
            upperLeftOffset: 190f, 
            viewportLeftOffset: 190f, 
            showBim: true, 
            showCuDan: false
        );
    }

    public void ShowUnitSearchState()
    {
        UpdateLayout(
            leftWidth: 400f, 
            upperLeftOffset: 400f, 
            viewportLeftOffset: 400f, 
            showBim: false, 
            showCuDan: true
        );
    }

    // --- HELPER LAYOUT FUNCTION ---

    private void UpdateLayout(float leftWidth, float upperLeftOffset, float viewportLeftOffset, bool showBim, bool showCuDan)
    {
        // 1. Set LeftPanel's Width
        leftPanel.sizeDelta = new Vector2(leftWidth, leftPanel.sizeDelta.y);

        // 2. Set UpperPanel's "Left" offset (offsetMin.x controls Left)
        upperPanel.offsetMin = new Vector2(upperLeftOffset, upperPanel.offsetMin.y);

        // 3. Set Viewport's "Left" offset
        viewportPanel.offsetMin = new Vector2(viewportLeftOffset, viewportPanel.offsetMin.y);

        // 4. Toggle internal panels
        if (panelBimInfo != null) panelBimInfo.SetActive(showBim);
        if (panelCuDan != null) panelCuDan.SetActive(showCuDan);
    }
}