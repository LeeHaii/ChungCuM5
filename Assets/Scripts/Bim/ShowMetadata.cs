using System.Collections.Generic;
using BimRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ShowMetadata : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private ScrollRect entriesPanel;
    private readonly List<BimMetadataRowView> entries = new List<BimMetadataRowView>(32);

    [Header("Position")]
    [SerializeField] private TextMeshProUGUI corHeader;
    [SerializeField] private TextMeshProUGUI xcor;
    [SerializeField] private TextMeshProUGUI ycor;
    [SerializeField] private TextMeshProUGUI zcor;

    [Header("Metadata Filtering")]
    [Tooltip("If key or value contains these strings, the entire entry is hidden.")]
    public List<string> keysToIgnore = new List<string>();

    [Tooltip("These strings will be removed from keys and values (e.g. 'IFCLIST/' -> '').")]
    public List<string> stringsToRemoveFromKeys = new List<string>();

    private Transform lastSelectedTransform;
    private int lastSelectedElementIndex = -1;
    private Camera mainCamera;
    private int activeEntryCount;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (entryPrefab != null) entryPrefab.SetActive(false);
        ClearUI();
    }

    private void Update()
    {
        if (mainCamera == null) return;

        bool isClicking = false;
        bool canUseHoverCache = false;
        Vector2 clickPosition = Vector2.zero;
        bool isOverUI = false;

        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                isClicking = true;
                clickPosition = touch.screenPosition;
                isOverUI = EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject(touch.touchId);
            }
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicking = true;
            canUseHoverCache = true;
            clickPosition = Mouse.current.position.ReadValue();
            isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        if (!isClicking || isOverUI) return;

        RaycastHit hit = default;
        bool usedHoverCache = canUseHoverCache
            && HoverManager.Instance != null
            && HoverManager.Instance.TryGetCachedRaycast(clickPosition, out hit);
        bool hitFound = usedHoverCache ? hit.collider != null : RaycastForSelection(clickPosition, out hit);
        if (!hitFound) return;

        if (TryGetHouseTarget(hit.collider.transform, out HouseComponent houseComponent))
        {
            if (houseComponent.transform != lastSelectedTransform || lastSelectedElementIndex != -1)
            {
                lastSelectedTransform = houseComponent.transform;
                lastSelectedElementIndex = -1;
                ShowHouseMetadata(houseComponent);
                OpenWindow();
            }

            return;
        }

        BimMetadataStore store = BimMetadataStore.Instance;
        if (store != null
            && store.TryGetElement(hit, out BimMetadataElement element)
            && (element.Target != lastSelectedTransform || element.ElementIndex != lastSelectedElementIndex))
        {
            lastSelectedTransform = element.Target;
            lastSelectedElementIndex = element.ElementIndex;
            ShowBimMetadata(element);
            OpenWindow();
        }
    }

    private bool RaycastForSelection(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        HoverManager hoverManager = HoverManager.Instance;
        if (hoverManager != null)
        {
            return Physics.Raycast(
                ray,
                out hit,
                hoverManager.MaxRaycastDistance,
                hoverManager.SelectableMask,
                QueryTriggerInteraction.Ignore);
        }

        return Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private static bool TryGetHouseTarget(Transform source, out HouseComponent houseComponent)
    {
        Transform current = source;
        for (int depth = 0; current != null && depth < 3; depth++, current = current.parent)
        {
            if (current.TryGetComponent(out houseComponent) && houseComponent.Data != null)
            {
                return true;
            }
        }

        houseComponent = source != null ? source.GetComponentInChildren<HouseComponent>(true) : null;
        return houseComponent != null && houseComponent.Data != null;
    }

    private void ShowHouseMetadata(HouseComponent houseComponent)
    {
        BeginPopulate(houseComponent.transform, houseComponent.Data.title);
        AddUIEntry("ID", houseComponent.Data.id.ToString());
        AddUIEntry("Price", string.Format("{0:N0} VND", houseComponent.Data.price));
        AddUIEntry("Description", houseComponent.Data.description);
        AddUIEntry("Area", houseComponent.Data.area_m2 + " m²");
        AddUIEntry("Status", houseComponent.Data.status);
        AddUIEntry("Residents", houseComponent.Data.residential_number.ToString());
        EndPopulate();
    }

    private void ShowBimMetadata(BimMetadataElement element)
    {
        BeginPopulate(element.Target, element.DisplayName);

        for (int i = 0; i < element.PropertyCount; i++)
        {
            if (!element.TryGetProperty(i, out string key, out string value) || ShouldIgnore(key, value))
            {
                continue;
            }

            RemoveConfiguredStrings(ref key, ref value);
            AddUIEntry(key, value);
        }

        EndPopulate();
    }

    private void BeginPopulate(Transform target, string title)
    {
        activeEntryCount = 0;
        if (corHeader != null) corHeader.SetText(string.IsNullOrEmpty(title) ? target.name : title);
        Vector3 position = target.position;
        if (xcor != null) xcor.SetText("X: {0:0}", position.x);
        if (ycor != null) ycor.SetText("Y: {0:0}", position.y);
        if (zcor != null) zcor.SetText("Z: {0:0}", position.z);
    }

    private void EndPopulate()
    {
        for (int i = activeEntryCount; i < entries.Count; i++)
        {
            entries[i].gameObject.SetActive(false);
        }
    }

    private bool ShouldIgnore(string key, string value)
    {
        for (int i = 0; i < keysToIgnore.Count; i++)
        {
            string ignored = keysToIgnore[i];
            if (!string.IsNullOrEmpty(ignored)
                && ((key?.Contains(ignored) ?? false) || (value?.Contains(ignored) ?? false)))
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveConfiguredStrings(ref string key, ref string value)
    {
        for (int i = 0; i < stringsToRemoveFromKeys.Count; i++)
        {
            string removed = stringsToRemoveFromKeys[i];
            if (string.IsNullOrEmpty(removed)) continue;
            key = key?.Replace(removed, string.Empty);
            value = value?.Replace(removed, string.Empty);
        }
    }

    private void AddUIEntry(string key, string value)
    {
        if (entryPrefab == null || entriesPanel == null || entriesPanel.content == null) return;

        BimMetadataRowView row;
        if (activeEntryCount < entries.Count)
        {
            row = entries[activeEntryCount];
        }
        else
        {
            GameObject instance = Instantiate(entryPrefab, entriesPanel.content);
            row = instance.GetComponent<BimMetadataRowView>();
            if (row == null) row = instance.AddComponent<BimMetadataRowView>();
            entries.Add(row);
        }

        row.Bind(key ?? string.Empty, value ?? string.Empty);
        row.gameObject.SetActive(true);
        activeEntryCount++;
    }

    private void OpenWindow()
    {
        Michsky.MUIP.ModalWindowManager window = GetComponent<Michsky.MUIP.ModalWindowManager>();
        if (window != null) window.OpenWindow();
    }

    private void ClearUI()
    {
        activeEntryCount = 0;
        EndPopulate();
        if (corHeader != null) corHeader.SetText("No Object Selected");
        if (xcor != null) xcor.SetText("X: --");
        if (ycor != null) ycor.SetText("Y: --");
        if (zcor != null) zcor.SetText("Z: --");

        Michsky.MUIP.ModalWindowManager window = GetComponent<Michsky.MUIP.ModalWindowManager>();
        if (window != null) window.CloseWindow();
    }
}
