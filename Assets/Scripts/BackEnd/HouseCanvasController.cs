using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class HouseCanvasController : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject uiPanel;

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI areaText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI residentsText;

    private RawImage houseImage;
    private Button goToApartmentButton;

    private Camera mainCamera;
    private HouseData currentHouseData;
    private GameObject currentSelectedUnit;

    [Header("Data & Hierarchy")]
    public Transform floorsParent;
    private Database.IQuanLyService _quanLyService;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiPanel != null)
        {
            var mwm = uiPanel.GetComponent<Michsky.MUIP.ModalWindowManager>();
            if (mwm != null) mwm.CloseWindow();
            else uiPanel.SetActive(false);
            
            // Auto-bind newly created UI elements via code
            houseImage = uiPanel.GetComponentInChildren<RawImage>();
            goToApartmentButton = uiPanel.GetComponentInChildren<Button>();
            
            if (goToApartmentButton != null)
            {
                goToApartmentButton.onClick.AddListener(OnGoToApartmentClicked);
            }
        }

        string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Database", "ChungCuM5.db");
        _quanLyService = new Database.SqliteQuanLyService(dbPath);

        if (floorsParent == null)
        {
            GameObject floorsObj = GameObject.Find("Floors");
            if (floorsObj != null) floorsParent = floorsObj.transform;
        }

        LoadSQLiteData();
    }

    private void LoadSQLiteData()
    {
        _quanLyService.GetDanhSachCanHo(
            onSuccess: (listCanHo) => {
                if (floorsParent == null) return;
                
                var dict = new System.Collections.Generic.Dictionary<string, Database.CanHo>();
                foreach (var c in listCanHo) dict[c.MaCanHo] = c;

                foreach (Transform floorTransform in floorsParent)
                {
                    foreach (Transform unitTransform in floorTransform)
                    {
                        if (unitTransform.CompareTag("Unit"))
                        {
                            string unitName = unitTransform.name; 
                            if (dict.ContainsKey(unitName))
                            {
                                var canHo = dict[unitName];
                                HouseData hd = new HouseData();
                                hd.id = int.TryParse(canHo.MaCanHo, out int id) ? id : 0;
                                hd.title = $"Căn hộ {canHo.MaCanHo}";
                                hd.price = 0; 
                                hd.description = $"Chủ sở hữu: {canHo.ChuSoHuu}\nSổ GCN: {canHo.SoGCN}";
                                hd.area_m2 = canHo.DienTich;
                                hd.status = string.IsNullOrEmpty(canHo.ChuSoHuu) ? "Chưa bán" : "Đã sở hữu";
                                hd.residential_number = 0; 
                                hd.addressable_str = "Apartment" + canHo.MaCanHo;

                                HouseComponent comp = unitTransform.gameObject.GetComponent<HouseComponent>();
                                if (comp == null) comp = unitTransform.gameObject.AddComponent<HouseComponent>();
                                comp.SetData(hd);

                                // Fetch residents to update residential number
                                _quanLyService.GetCuDanTheoCanHo(canHo.MaCanHo, 
                                    (residents) => {
                                        hd.residential_number = residents.Count;
                                    }, 
                                    (err) => {}
                                );
                            }
                        }
                    }
                }
            },
            onError: (err) => {
                Debug.LogError("SQLite Fetch Error: " + err);
            }
        );
    }

    void Update()
    {
        bool wasPressed = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wasPressed = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            wasPressed = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (wasPressed)
        {
            HandleClick(screenPos);
        }

        // Auto-update if data arrives while Skeleton UI is showing
        if (uiPanel != null && uiPanel.activeSelf && currentHouseData == null && currentSelectedUnit != null)
        {
            if (currentSelectedUnit.TryGetComponent(out HouseComponent houseComp) && houseComp.Data != null)
            {
                ShowHouseData(houseComp.Data);
            }
        }
    }

    private void HandleClick(Vector2 screenPos)
    {
        // Ignore click if the pointer is over a UI element
        if (EventSystem.current != null)
        {
            bool isOverUI = false;
            
            // Check for touch UI hit
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            }
            
            // Fallback to general pointer check (Mouse)
            if (!isOverUI)
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject();
            }

            if (isOverUI) return;
        }

        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (clickedObject.CompareTag("Unit"))
            {
                currentSelectedUnit = clickedObject;
                if (clickedObject.TryGetComponent(out HouseComponent houseComp) && houseComp.Data != null)
                {
                    ShowHouseData(houseComp.Data);
                }
                else
                {
                    ShowSkeletonUI();
                }
                return;
            }
        }

        // If we click nothing or something else, hide the UI
        HideUI();
    }

    private void ShowSkeletonUI()
    {
        currentHouseData = null;

        if (titleText != null) titleText.text = "ĐANG TẢI...";
        if (priceText != null) priceText.text = "GIÁ BÁN: ---";
        if (areaText != null) areaText.text = "DIỆN TÍCH: ---";
        if (descText != null) descText.text = "MÔ TẢ: ĐANG TẢI...";
        if (statusText != null) statusText.text = "TRẠNG THÁI: ---";
        if (residentsText != null) residentsText.text = "SỐ NGƯỜI Ở: ---";

        if (houseImage != null)
        {
            houseImage.texture = null; // Clear image while loading
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(true); // Must be active for Animator to work!
            var mwm = uiPanel.GetComponent<Michsky.MUIP.ModalWindowManager>();
            if (mwm != null) mwm.OpenWindow();
        }
    }

    private void ShowHouseData(HouseData data)
    {
        currentHouseData = data;

        if (titleText != null) titleText.text = data.title;
        if (priceText != null) priceText.text = $"GIÁ BÁN: {data.price:N0} VND";
        if (areaText != null) areaText.text = $"DIỆN TÍCH: {data.area_m2} m²";
        if (descText != null) descText.text = $"MÔ TẢ: {data.description}";
        if (statusText != null) statusText.text = $"TRẠNG THÁI: {data.status}";
        if (residentsText != null) residentsText.text = $"SỐ NGƯỜI Ở: {data.residential_number}";

        if (houseImage != null && !string.IsNullOrEmpty(data.image_url))
        {
            StartCoroutine(LoadImageCoroutine(data.image_url));
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(true); // Must be active for Animator to work!
            var mwm = uiPanel.GetComponent<Michsky.MUIP.ModalWindowManager>();
            if (mwm != null) mwm.OpenWindow();
        }
    }

    private IEnumerator LoadImageCoroutine(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                if (houseImage != null)
                {
                    houseImage.texture = texture;
                }
            }
            else
            {
                Debug.LogError("Failed to download image: " + uwr.error);
            }
        }
    }

    public void OnGoToApartmentClicked()
    {
        if (currentHouseData != null && !string.IsNullOrEmpty(currentHouseData.addressable_str))
        {
            PlayerPrefs.SetString("SelectedApartment", currentHouseData.addressable_str);
            PlayerPrefs.Save();
            SceneManager.LoadScene("ApartmentScene");
        }
        else
        {
            Debug.LogWarning("No apartment selected or missing addressable_str.");
        }
    }

    private void HideUI()
    {
        currentSelectedUnit = null;
        if (uiPanel != null)
        {
            var mwm = uiPanel.GetComponent<Michsky.MUIP.ModalWindowManager>();
            if (mwm != null) mwm.CloseWindow();
            else uiPanel.SetActive(false);
        }
    }
}
