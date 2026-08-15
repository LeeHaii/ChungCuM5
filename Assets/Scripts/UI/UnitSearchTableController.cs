using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UnitSearchTableController : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform contentPanel;

    [Header("Family Data Link")]
    [SerializeField] private FamilyDataViewController familyDataController;

    private readonly List<UnitSearchRowView> rows = new List<UnitSearchRowView>(224);
    private Database.IQuanLyService quanLyService;
    private bool isDataLoaded;

    private void Awake()
    {
        string dbPath = Path.Combine(Application.streamingAssetsPath, "Database", "ChungCuM5.db");
        quanLyService = new Database.SqliteQuanLyService(dbPath);
        if (entryPrefab != null) entryPrefab.SetActive(false);
    }

    private void OnEnable()
    {
        if (!isDataLoaded) LoadData();
    }

    public void LoadData()
    {
        if (quanLyService == null) return;

        quanLyService.GetDanhSachCanHo(
            units =>
            {
                PopulateTable(units);
                isDataLoaded = true;
            },
            err => Debug.LogError("Error loading data for table: " + err));
    }

    private void PopulateTable(List<Database.CanHo> units)
    {
        if (entryPrefab == null || contentPanel == null) return;

        int requiredCount = units?.Count ?? 0;
        for (int i = rows.Count; i < requiredCount; i++)
        {
            GameObject instance = Instantiate(entryPrefab, contentPanel);
            UnitSearchRowView row = instance.GetComponent<UnitSearchRowView>();
            if (row == null) row = instance.AddComponent<UnitSearchRowView>();
            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool active = i < requiredCount;
            UnitSearchRowView row = rows[i];
            if (active) row.Bind(units[i], familyDataController);
            row.gameObject.SetActive(active);
        }
    }
}
