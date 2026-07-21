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
        Vector2 screenPos = Vector2.zero;

        // Read input from the new Input System
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
    }

    private void HandleClick(Vector2 screenPos)
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

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
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
}
