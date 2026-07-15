using UnityEngine;

public class CameraViewportAligner : MonoBehaviour
{
    public Camera targetCamera;
    public RectTransform uiPlaceholderPanel;

    void Update()
    {
        if (targetCamera == null || uiPlaceholderPanel == null) return;

        Vector3[] corners = new Vector3[4];
        uiPlaceholderPanel.GetWorldCorners(corners);

        float x = corners[0].x / Screen.width;
        float y = corners[0].y / Screen.height;
        float w = (corners[2].x - corners[0].x) / Screen.width;
        float h = (corners[2].y - corners[0].y) / Screen.height;

        targetCamera.rect = new Rect(x, y, w, h);
    }
}
