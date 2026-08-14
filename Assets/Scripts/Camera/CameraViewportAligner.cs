using UnityEngine;

public class CameraViewportAligner : MonoBehaviour
{
    public Camera targetCamera;
    public RectTransform uiPlaceholderPanel;

    private readonly Vector3[] corners = new Vector3[4];

    private Camera lastTargetCamera;
    private RectTransform lastPlaceholderPanel;
    private Rect lastPanelRect;
    private Matrix4x4 lastPanelMatrix;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private bool isDirty = true;

    private void OnEnable()
    {
        Canvas.willRenderCanvases += UpdateViewportIfNeeded;
        isDirty = true;
    }

    private void Start()
    {
        UpdateViewportIfNeeded();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateViewportIfNeeded;
    }

    private void OnRectTransformDimensionsChange()
    {
        isDirty = true;
    }

    private void UpdateViewportIfNeeded()
    {
        if (targetCamera == null || uiPlaceholderPanel == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect panelRect = uiPlaceholderPanel.rect;
        Matrix4x4 panelMatrix = uiPlaceholderPanel.localToWorldMatrix;
        bool referencesChanged = targetCamera != lastTargetCamera || uiPlaceholderPanel != lastPlaceholderPanel;
        bool resolutionChanged = Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;
        bool geometryChanged = panelRect != lastPanelRect || panelMatrix != lastPanelMatrix;

        if (!isDirty && !referencesChanged && !resolutionChanged && !geometryChanged)
        {
            return;
        }

        uiPlaceholderPanel.GetWorldCorners(corners);

        float x = corners[0].x / Screen.width;
        float y = corners[0].y / Screen.height;
        float w = (corners[2].x - corners[0].x) / Screen.width;
        float h = (corners[2].y - corners[0].y) / Screen.height;

        Rect nextRect = new Rect(x, y, w, h);
        if (!Approximately(targetCamera.rect, nextRect))
        {
            targetCamera.rect = nextRect;
        }

        lastTargetCamera = targetCamera;
        lastPlaceholderPanel = uiPlaceholderPanel;
        lastPanelRect = panelRect;
        lastPanelMatrix = panelMatrix;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        isDirty = false;
    }

    private static bool Approximately(Rect a, Rect b)
    {
        return Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y)
            && Mathf.Approximately(a.width, b.width)
            && Mathf.Approximately(a.height, b.height);
    }
}
