using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class FamilyDataViewController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent transform containing EntryElementGD items")]
    public Transform panelThongTinHoContent;
    
    [Tooltip("The prefab for a single resident entry (EntryElementGD)")]
    public GameObject entryElementGDPrefab;

    [Tooltip("The whole Panel Thong Tin Ho to show/hide")]
    public GameObject panelThongTinHo;

    [Header("Highlight Settings")]
    [Tooltip("YellowTransparent material from the Materials folder")]
    public Material yellowTransparentMaterial;

    [SerializeField] private Button buttonCollapse;

    private Database.IQuanLyService _quanLyService;
    
    // Store original materials to reset highlighting
    private Dictionary<GameObject, Material[]> _originalMaterials = new Dictionary<GameObject, Material[]>();
    private GameObject _currentlyHighlightedUnit = null;

    private void Awake()
    {
        string dbPath = Path.Combine(Application.streamingAssetsPath, "Database", "ChungCuM5.db");
        _quanLyService = new Database.SqliteQuanLyService(dbPath);
    }

    private void Start()
    {
        if(panelThongTinHo != null) panelThongTinHo.SetActive(false);
        if(buttonCollapse != null) buttonCollapse.onClick.AddListener(() => {panelThongTinHo.SetActive(false);});
    }

    /// <summary>
    /// Call this method from the ButtonViewData onClick event, passing the MaCanHo string.
    /// </summary>
    public void OnViewFamilyData(string maCanHo)
    {
        if (panelThongTinHo != null)
            panelThongTinHo.SetActive(true);

        LoadResidents(maCanHo);
        HighlightUnit(maCanHo);
    }

    private void LoadResidents(string maCanHo)
    {
        if (_quanLyService == null) return;

        _quanLyService.GetCuDanTheoCanHo(maCanHo,
            onSuccess: (listCuDan) => {
                PopulateResidents(listCuDan);
            },
            onError: (err) => {
                Debug.LogError("Error loading residents for " + maCanHo + ": " + err);
            });
    }

    private void PopulateResidents(List<Database.CuDan> listCuDan)
    {
        if (entryElementGDPrefab == null || panelThongTinHoContent == null) return;

        // Clear existing children except the template/header
        foreach (Transform child in panelThongTinHoContent)
        {
            if (child.gameObject != entryElementGDPrefab && child.name != "Header")
            {
                Destroy(child.gameObject);
            }
        }

        entryElementGDPrefab.SetActive(false);

        foreach (var cuDan in listCuDan)
        {
            GameObject newEntry = Instantiate(entryElementGDPrefab, panelThongTinHoContent);
            newEntry.SetActive(true);

            SetText(newEntry, "HoTen", cuDan.HoTen);
            SetText(newEntry, "SoCCCD", cuDan.SoCCCD);
            SetText(newEntry, "NgaySinh", cuDan.NgaySinh);
            SetText(newEntry, "SDT", cuDan.SDT);
            SetText(newEntry, "Email", cuDan.Email);
            SetText(newEntry, "GioiTinh", cuDan.GioiTinh);
            SetText(newEntry, "QuanHeVoiChuHo", cuDan.QuanHeVoiChuHo);
            SetText(newEntry, "LoaiCuTru", cuDan.LoaiCuTru);
            SetText(newEntry, "TrangThai", cuDan.TrangThai);
        }
    }

    private void HighlightUnit(string maCanHo)
    {
        // Revert previous highlight
        if (_currentlyHighlightedUnit != null && _originalMaterials.ContainsKey(_currentlyHighlightedUnit))
        {
            var prevRenderer = _currentlyHighlightedUnit.GetComponent<Renderer>();
            if (prevRenderer != null)
            {
                prevRenderer.materials = _originalMaterials[_currentlyHighlightedUnit];
            }
            _currentlyHighlightedUnit = null;
        }

        // Find the GameObject matching MaCanHo. 
        GameObject targetUnit = GameObject.Find(maCanHo);
        
        if (targetUnit == null)
        {
            Debug.LogWarning($"GameObject with name '{maCanHo}' not found in the scene.");
            return;
        }

        _currentlyHighlightedUnit = targetUnit;
        var renderer = targetUnit.GetComponent<Renderer>();

        if (renderer != null && yellowTransparentMaterial != null)
        {
            // Save original materials
            if (!_originalMaterials.ContainsKey(targetUnit))
            {
                _originalMaterials[targetUnit] = renderer.materials;
            }

            // Apply YellowTransparent material to all material slots
            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = yellowTransparentMaterial;
            }
            renderer.materials = newMaterials;
        }
        else
        {
            Debug.LogWarning($"Target unit '{maCanHo}' has no Renderer or YellowTransparent material is not assigned.");
        }
    }

    private void SetText(GameObject entry, string childName, string value)
    {
        Transform child = entry.transform.Find(childName);
        if (child != null)
        {
            var textComp = child.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = string.IsNullOrEmpty(value) ? "-" : value;
            }
        }
    }
}
