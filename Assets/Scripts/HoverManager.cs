using BimRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HoverManager : MonoBehaviour
{
    private enum HighlightType { None, Darken }

    [Header("Tag Highlight (Units)")]
    public string targetTag = "Unit";

    [Header("Component Highlight (Metadata)")]
    [Range(0f, 1f)]
    public float darkenMultiplier = 0.7f;

    [Header("Raycast")]
    [SerializeField] private LayerMask selectableMask = ~0;
    [SerializeField, Min(0.01f)] private float maxRaycastDistance = 5000f;

    public BimDataProperties bimDataProperties;

    private Transform currentHoveredObject;
    private int currentHoveredElementIndex = -1;
    private Renderer currentRenderer;
    private HighlightType currentHighlightType = HighlightType.None;

    private Camera mainCamera;
    private MaterialPropertyBlock propertyBlock;
    private Vector2 lastPointerPosition;
    private RaycastHit lastRaycastHit;
    private bool hasPointerPosition;
    private bool hasCachedRaycast;
    private bool wasPointerOverUI;
    private bool hasPointerOverUIState;

    public static HoverManager Instance { get; private set; }
    public Renderer CurrentHoveredRenderer => currentRenderer;
    public float DarkenMultiplier => darkenMultiplier;
    public LayerMask SelectableMask => selectableMask;
    public float MaxRaycastDistance => maxRaycastDistance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        propertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool pointerMoved = !hasPointerPosition || mousePos != lastPointerPosition;
        bool uiStateChanged = !hasPointerOverUIState || isPointerOverUI != wasPointerOverUI;

        if (!pointerMoved && !uiStateChanged)
        {
            return;
        }

        lastPointerPosition = mousePos;
        hasPointerPosition = true;
        wasPointerOverUI = isPointerOverUI;
        hasPointerOverUIState = true;

        if (isPointerOverUI)
        {
            hasCachedRaycast = false;
            RemoveHighlight();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        bool hitFound = Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxRaycastDistance,
            selectableMask,
            QueryTriggerInteraction.Ignore);

        lastRaycastHit = hitFound ? hit : default;
        hasCachedRaycast = true;

        if (hitFound)
        {
            Transform hitTransform = hit.transform;
            BimMetadataStore store = BimMetadataStore.Instance;
            bool isUnit = hitTransform.CompareTag(targetTag);
            BimMetadataElement element = default;
            bool hasMetadata = store != null && store.TryGetElement(hit, out element);
            Transform highlightTarget = hasMetadata ? element.Target : hitTransform;
            int elementIndex = hasMetadata ? element.ElementIndex : -1;

            if (highlightTarget != currentHoveredObject || elementIndex != currentHoveredElementIndex)
            {
                RemoveHighlight();

                if (isUnit)
                {
                    ApplyDarken(hitTransform, hitTransform.GetComponent<Renderer>(), -1);
                }
                else if (hasMetadata && bimDataProperties != null && bimDataProperties.GetBIMdata())
                {
                    Renderer targetRenderer = highlightTarget.GetComponent<Renderer>();
                    if (targetRenderer == null) targetRenderer = hitTransform.GetComponent<Renderer>();
                    ApplyDarken(highlightTarget, targetRenderer, elementIndex);
                }
            }
        }
        else
        {
            RemoveHighlight();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        hasCachedRaycast = false;
        hasPointerPosition = false;
        hasPointerOverUIState = false;
        RemoveHighlight();
    }

    public bool TryGetCachedRaycast(Vector2 screenPosition, out RaycastHit hit)
    {
        hit = lastRaycastHit;
        return hasCachedRaycast
            && hasPointerPosition
            && (screenPosition - lastPointerPosition).sqrMagnitude <= 0.01f;
    }

    private void ApplyDarken(Transform obj, Renderer targetRenderer, int elementIndex)
    {
        currentHoveredObject = obj;
        currentHoveredElementIndex = elementIndex;
        currentRenderer = targetRenderer;

        if (currentRenderer != null)
        {
            currentHighlightType = HighlightType.Darken;

            Color originalColor = Color.white;
            Material sharedMaterial = currentRenderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                return;
            }

            if (sharedMaterial.HasProperty("_BaseColor"))
                originalColor = sharedMaterial.GetColor("_BaseColor");
            else if (sharedMaterial.HasProperty("_Color"))
                originalColor = sharedMaterial.color;

            Color darkenedColor = new Color(
                originalColor.r * darkenMultiplier,
                originalColor.g * darkenMultiplier,
                originalColor.b * darkenMultiplier,
                originalColor.a
            );

            currentRenderer.GetPropertyBlock(propertyBlock);

            if (sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", darkenedColor);
            else
                propertyBlock.SetColor("_Color", darkenedColor);

            currentRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RemoveHighlight()
    {
        if (currentHoveredObject != null && currentRenderer != null)
        {
            if (currentHighlightType == HighlightType.Darken)
            {
                currentRenderer.SetPropertyBlock(null);
            }
        }

        currentHoveredObject = null;
        currentHoveredElementIndex = -1;
        currentRenderer = null;
        currentHighlightType = HighlightType.None;
    }
}
