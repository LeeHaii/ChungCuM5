using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnitSearchTableController : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform contentPanel;

    private Database.IQuanLyService _quanLyService;
    private bool isDataLoaded = false;

    private void Awake()
    {
        string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Database", "ChungCuM5.db");
        _quanLyService = new Database.SqliteQuanLyService(dbPath);
    }

    private void OnEnable()
    {
        if (!isDataLoaded)
        {
            LoadData();
        }
    }

    public void LoadData()
    {
        if (_quanLyService == null) return;

        _quanLyService.GetDanhSachCanHo(
            onSuccess: (listCanHo) => {
                PopulateTable(listCanHo);
                isDataLoaded = true;
            },
            onError: (err) => {
                Debug.LogError("Error loading data for table: " + err);
            }
        );
    }

    private void PopulateTable(List<Database.CanHo> listCanHo)
    {
        if (entryPrefab == null || contentPanel == null) return;

        // Clear existing children except the prefab and Header
        foreach (Transform child in contentPanel)
        {
            if (child.gameObject != entryPrefab && child.name != "Header")
            {
                Destroy(child.gameObject);
            }
        }

        entryPrefab.SetActive(false); // Hide the prefab template

        foreach (var canHo in listCanHo)
        {
            GameObject newEntry = Instantiate(entryPrefab, contentPanel);
            newEntry.SetActive(true);

            string tenCanHo = string.IsNullOrEmpty(canHo.TenCanHo) ? canHo.MaCanHo : canHo.TenCanHo;

            SetText(newEntry, "TenCanHo", tenCanHo);
            SetText(newEntry, "DiaChi_ToaNha", canHo.DiaChi_ToaNha);
            SetText(newEntry, "DienTich", canHo.DienTich.ToString()); // just number or add m2 depending on UI space
            SetText(newEntry, "ChuSoHuu", canHo.ChuSoHuu);
            SetText(newEntry, "SoGCN", canHo.SoGCN);
            SetText(newEntry, "ThoiHanSoHuu", canHo.ThoiHanSoHuu);
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
