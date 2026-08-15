using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FamilyDataViewController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("UI References")]
    [Tooltip("The parent transform containing EntryElementGD items")]
    public Transform panelThongTinHoContent;

    [Tooltip("The prefab for a single resident entry (EntryElementGD)")]
    public GameObject entryElementGDPrefab;

    [Tooltip("The whole Panel Thong Tin Ho to show/hide")]
    public GameObject panelThongTinHo;

    [Header("Highlight Settings")]
    [Tooltip("YellowTransparent material used as the source for the unit highlight color")]
    public Material yellowTransparentMaterial;

    [SerializeField] private Button buttonCollapse;

    private readonly List<ResidentRowView> residentRows = new List<ResidentRowView>(8);
    private readonly Dictionary<string, Renderer> unitRenderers =
        new Dictionary<string, Renderer>(224, System.StringComparer.Ordinal);

    private MaterialPropertyBlock originalUnitBlock;
    private MaterialPropertyBlock highlightUnitBlock;
    private Database.IQuanLyService quanLyService;
    private Renderer currentlyHighlightedRenderer;

    private void Awake()
    {
        originalUnitBlock = new MaterialPropertyBlock();
        highlightUnitBlock = new MaterialPropertyBlock();
        string dbPath = Path.Combine(Application.streamingAssetsPath, "Database", "ChungCuM5.db");
        quanLyService = new Database.SqliteQuanLyService(dbPath);
        CacheUnitRenderers();
    }

    private void Start()
    {
        if (panelThongTinHo != null) panelThongTinHo.SetActive(false);
        if (entryElementGDPrefab != null) entryElementGDPrefab.SetActive(false);
        if (buttonCollapse != null) buttonCollapse.onClick.AddListener(Collapse);
    }

    private void OnDestroy()
    {
        if (buttonCollapse != null) buttonCollapse.onClick.RemoveListener(Collapse);
        RestoreHighlightedUnit();
    }

    public void OnViewFamilyData(string maCanHo)
    {
        if (panelThongTinHo != null) panelThongTinHo.SetActive(true);
        LoadResidents(maCanHo);
        HighlightUnit(maCanHo);
    }

    private void Collapse()
    {
        if (panelThongTinHo != null) panelThongTinHo.SetActive(false);
        RestoreHighlightedUnit();
    }

    private void LoadResidents(string maCanHo)
    {
        if (quanLyService == null) return;

        quanLyService.GetCuDanTheoCanHo(
            maCanHo,
            PopulateResidents,
            err => Debug.LogError("Error loading residents for " + maCanHo + ": " + err));
    }

    private void PopulateResidents(List<Database.CuDan> residents)
    {
        if (entryElementGDPrefab == null || panelThongTinHoContent == null) return;

        int requiredCount = residents?.Count ?? 0;
        for (int i = residentRows.Count; i < requiredCount; i++)
        {
            GameObject instance = Instantiate(entryElementGDPrefab, panelThongTinHoContent);
            ResidentRowView row = instance.GetComponent<ResidentRowView>();
            if (row == null) row = instance.AddComponent<ResidentRowView>();
            residentRows.Add(row);
        }

        for (int i = 0; i < residentRows.Count; i++)
        {
            bool active = i < requiredCount;
            ResidentRowView row = residentRows[i];
            if (active) row.Bind(residents[i]);
            row.gameObject.SetActive(active);
        }
    }

    private void CacheUnitRenderers()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null
                && renderer.CompareTag("Unit")
                && !unitRenderers.ContainsKey(renderer.gameObject.name))
            {
                unitRenderers.Add(renderer.gameObject.name, renderer);
            }
        }
    }

    private void HighlightUnit(string maCanHo)
    {
        RestoreHighlightedUnit();

        if (string.IsNullOrEmpty(maCanHo)
            || !unitRenderers.TryGetValue(maCanHo, out Renderer renderer)
            || renderer == null)
        {
            Debug.LogWarning("Unit renderer '" + maCanHo + "' was not found in the scene.", this);
            return;
        }

        if (yellowTransparentMaterial == null)
        {
            Debug.LogWarning("YellowTransparent material is not assigned.", this);
            return;
        }

        currentlyHighlightedRenderer = renderer;
        renderer.GetPropertyBlock(originalUnitBlock);
        renderer.GetPropertyBlock(highlightUnitBlock);

        Material targetMaterial = renderer.sharedMaterial;
        if (targetMaterial != null && targetMaterial.HasProperty(BaseColorId))
        {
            Color highlightColor = yellowTransparentMaterial.HasProperty(BaseColorId)
                ? yellowTransparentMaterial.GetColor(BaseColorId)
                : yellowTransparentMaterial.color;
            highlightUnitBlock.SetColor(BaseColorId, highlightColor);
        }
        else if (targetMaterial != null && targetMaterial.HasProperty(ColorId))
        {
            highlightUnitBlock.SetColor(ColorId, yellowTransparentMaterial.color);
        }

        renderer.SetPropertyBlock(highlightUnitBlock);
    }

    private void RestoreHighlightedUnit()
    {
        if (currentlyHighlightedRenderer == null) return;
        currentlyHighlightedRenderer.SetPropertyBlock(originalUnitBlock);
        currentlyHighlightedRenderer = null;
        originalUnitBlock.Clear();
        highlightUnitBlock.Clear();
    }
}
