using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Family Data Link")]
    public FamilyDataViewController familyDataController;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        bool wasPressed = false;
        bool canUseHoverCache = false;
        Vector2 screenPos = Vector2.zero;

        // Read input from the new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wasPressed = true;
            canUseHoverCache = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            wasPressed = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (wasPressed)
        {
            HandleClick(screenPos, canUseHoverCache);
        }
    }

    private void HandleClick(Vector2 screenPos, bool canUseHoverCache)
    {
        // Ignore click if the pointer is over a UI element
        if (EventSystem.current != null)
        {
            bool isOverUI = false;
            
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            }
            
            if (!isOverUI)
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject();
            }

            if (isOverUI) return;
        }

        if (_mainCamera == null) return;

        RaycastHit hit = default;
        bool usedHoverCache = canUseHoverCache
            && HoverManager.Instance != null
            && HoverManager.Instance.TryGetCachedRaycast(screenPos, out hit);

        bool hitFound = usedHoverCache ? hit.collider != null : RaycastForSelection(screenPos, out hit);
        if (hitFound)
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (clickedObject.CompareTag("Unit"))
            {
                if (familyDataController != null)
                {
                    familyDataController.OnViewFamilyData(clickedObject.name);
                }
            }
        }
    }

    private bool RaycastForSelection(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
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
}
