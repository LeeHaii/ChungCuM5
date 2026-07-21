using UnityEngine;

public class HeaderSync : MonoBehaviour
{
    public RectTransform contentToSyncWith;
    private RectTransform _rect;

    void Start()
    {
        _rect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (contentToSyncWith != null && _rect != null)
        {
            Vector2 pos = _rect.anchoredPosition;
            pos.x = contentToSyncWith.anchoredPosition.x;
            _rect.anchoredPosition = pos;

            Vector2 sd = _rect.sizeDelta;
            sd.x = contentToSyncWith.rect.width;
            _rect.sizeDelta = sd;
        }
    }
}
