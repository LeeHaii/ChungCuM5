using UnityEngine;
using UnityEngine.UI;

public class HeaderSync : MonoBehaviour
{
    public RectTransform contentToSyncWith;
    [SerializeField] private ScrollRect scrollRect;

    private RectTransform _rect;
    private bool isSubscribed;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
        }

        if (scrollRect == null && contentToSyncWith != null)
        {
            scrollRect = contentToSyncWith.GetComponentInParent<ScrollRect>();
        }

        Subscribe();
        SyncIfChanged();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnRectTransformDimensionsChange()
    {
        SyncIfChanged();
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        RectTransform.reapplyDrivenProperties += OnReapplyDrivenProperties;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        RectTransform.reapplyDrivenProperties -= OnReapplyDrivenProperties;
        isSubscribed = false;
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        SyncIfChanged();
    }

    private void OnReapplyDrivenProperties(RectTransform drivenTransform)
    {
        if (drivenTransform == contentToSyncWith)
        {
            SyncIfChanged();
        }
    }

    private void SyncIfChanged()
    {
        if (contentToSyncWith != null && _rect != null)
        {
            float targetX = contentToSyncWith.anchoredPosition.x;
            if (!Mathf.Approximately(_rect.anchoredPosition.x, targetX))
            {
                Vector2 position = _rect.anchoredPosition;
                position.x = targetX;
                _rect.anchoredPosition = position;
            }

            float targetWidth = contentToSyncWith.rect.width;
            if (!Mathf.Approximately(_rect.sizeDelta.x, targetWidth))
            {
                Vector2 sizeDelta = _rect.sizeDelta;
                sizeDelta.x = targetWidth;
                _rect.sizeDelta = sizeDelta;
            }
        }
    }
}
