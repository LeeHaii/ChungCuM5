using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// We need this specific library for modern mobile touch controls
using UnityEngine.InputSystem.EnhancedTouch; 
// We use an alias here so Unity doesn't confuse it with the old legacy touch system
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;

public class OrbitCamera : MonoBehaviour
{
    [Header("Touch Mode UI")]
    public GameObject touchControlsPanel;
    public Button touchModeButton;
    public Sprite imgOrbit;
    public Sprite imgDrag;

    [Header("Target Pivot")]
    public Vector3 pivotPoint = Vector3.zero;

    [Header("Distance & Zoom")]
    public float distance = 10.0f;
    public float minDistance = 2.0f;
    public float maxDistance = 30.0f;
    
    [Header("Mouse Speeds")]
    public float mouseZoomSpeed = 0.02f;
    public float mouseXSpeed = 0.2f;
    public float mouseYSpeed = 0.2f;
    public float mouseDragYSpeed = 0.05f;

    [Header("Touch Speeds")]
    public float touchZoomSpeed = 0.01f;
    public float touchXSpeed = 0.1f;
    public float touchYSpeed = 0.1f;
    public float touchDragYSpeed = 0.05f;

    [Header("Rotation Limits")]
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    [Header("Drag Limits")]
    public float dragYMinLimit = -10f;
    public float dragYMaxLimit = 30f;

    [Header("Collision")]
    public string collisionTag = "BackgroundWall";

    public enum TouchMode { Rotate, Drag }
    public TouchMode currentTouchMode = TouchMode.Rotate;

    private float x = 0.0f;
    private float y = 0.0f;
    
    private Vector3 initialPivotPoint;
    private float initialDistance;
    private float initialX;
    private float initialY;

    // --- ENABLING ENHANCED TOUCH ---
    // Enhanced touch must be explicitly turned on and off
    private void OnEnable() { EnhancedTouchSupport.Enable(); }
    private void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        initialPivotPoint = pivotPoint;
        initialDistance = distance;
        initialX = x;
        initialY = y;

        GameObject resetBtnObj = GameObject.Find("ButtonResetVIew");
        if (resetBtnObj == null)
        {
            resetBtnObj = GameObject.Find("ButtonResetView");
        }

        if (resetBtnObj != null)
        {
            Button btn = resetBtnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(ResetView);
            }
        }

        if (touchControlsPanel != null)
        {
            if (Touchscreen.current != null)
            {
                touchControlsPanel.SetActive(true);
                if (touchModeButton != null)
                {
                    touchModeButton.onClick.AddListener(ToggleTouchMode);
                    UpdateTouchModeImage();
                }
            }
            else
            {
                touchControlsPanel.SetActive(false);
            }
        }

        UpdateCameraPosition();
    }

    public void ToggleTouchMode()
    {
        currentTouchMode = currentTouchMode == TouchMode.Rotate ? TouchMode.Drag : TouchMode.Rotate;
        UpdateTouchModeImage();
    }

    private void UpdateTouchModeImage()
    {
        if (imgDrag == null || imgOrbit == null) return;
        if (currentTouchMode == TouchMode.Drag) touchModeButton.image.sprite = imgDrag;
        else touchModeButton.image.sprite = imgOrbit;
    }

    public void ResetView()
    {
        pivotPoint = initialPivotPoint;
        distance = initialDistance;
        x = initialX;
        y = initialY;
        UpdateCameraPosition();
    }

    void LateUpdate()
    {
        Vector2 orbitDelta = Vector2.zero;
        float dragDeltaY = 0f;

        bool isOverUI = false;
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                isOverUI = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue()))
                {
                    isOverUI = true;
                }
            }
        }

        if (!isOverUI)
        {
            // ==========================================
            // 1. TOUCH CONTROLS (Mobile / Web on Phone)
            // ==========================================
            if (Touch.activeTouches.Count > 0)
            {
                // ONE FINGER: Rotate or Drag
                if (Touch.activeTouches.Count == 1)
                {
                    if (currentTouchMode == TouchMode.Rotate)
                    {
                        orbitDelta = Touch.activeTouches[0].delta * touchXSpeed; // X and Y are handled below
                    }
                    else
                    {
                        dragDeltaY = Touch.activeTouches[0].delta.y * touchDragYSpeed;
                    }
                }
                // TWO FINGERS: Pinch to Zoom
                else if (Touch.activeTouches.Count == 2)
                {
                    Touch touchZero = Touch.activeTouches[0];
                    Touch touchOne = Touch.activeTouches[1];

                    // Find the position of the touches in the previous frame
                    Vector2 touchZeroPrevPos = touchZero.screenPosition - touchZero.delta;
                    Vector2 touchOnePrevPos = touchOne.screenPosition - touchOne.delta;

                    // Find the distance between the touches in the previous and current frames
                    float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float currentMagnitude = (touchZero.screenPosition - touchOne.screenPosition).magnitude;

                    // The difference in magnitude is our zoom delta
                    float difference = currentMagnitude - prevMagnitude;

                    // Apply zoom (If distance increases, we zoom in, so we subtract)
                    distance = Mathf.Clamp(distance - (difference * touchZoomSpeed), minDistance, maxDistance);
                }
            }
            // ==========================================
            // 2. MOUSE CONTROLS (PC / Web on Desktop)
            // ==========================================
            else if (Mouse.current != null)
            {
                // Right Click: Rotate or Drag
                if (Mouse.current.rightButton.isPressed)
                {
                    if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                    {
                        dragDeltaY = Mouse.current.delta.y.ReadValue() * mouseDragYSpeed;
                    }
                    else
                    {
                        orbitDelta = Mouse.current.delta.ReadValue() * mouseXSpeed;
                    }
                }

                // Scroll Wheel: Zoom
                float scroll = Mouse.current.scroll.y.ReadValue();
                if (scroll != 0.0f)
                {
                    distance = Mathf.Clamp(distance - (scroll * mouseZoomSpeed), minDistance, maxDistance);
                }
            }
        }

        // ==========================================
        // 3. APPLY ROTATION AND POSITION
        // ==========================================
        if (orbitDelta != Vector2.zero)
        {
            // Apply the delta to our angles. 
            // Notice we use the same variables whether it came from a mouse or a finger.
            x += orbitDelta.x;
            y -= orbitDelta.y; // Inverted so dragging down looks up

            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }

        if (dragDeltaY != 0)
        {
            pivotPoint.y -= dragDeltaY;
            pivotPoint.y = Mathf.Clamp(pivotPoint.y, dragYMinLimit, dragYMaxLimit);
        }

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = pivotPoint + direction * distance;

        RaycastHit[] hits = Physics.RaycastAll(pivotPoint, direction, distance);
        float closestDistance = distance;
        bool hitWall = false;
        Vector3 wallHitPoint = Vector3.zero;

        foreach (RaycastHit hit in hits)
        {
            if (!string.IsNullOrEmpty(collisionTag) && hit.collider.CompareTag(collisionTag))
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitWall = true;
                    wallHitPoint = hit.point;
                }
            }
        }

        if (hitWall)
        {
            // Prevent clipping into the wall by offsetting slightly
            desiredPosition = wallHitPoint - direction * 0.2f;
        }

        transform.rotation = rotation;
        transform.position = desiredPosition;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}