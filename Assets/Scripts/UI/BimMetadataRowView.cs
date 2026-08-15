using TMPro;
using UnityEngine;

public sealed class BimMetadataRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI valueText;

    public void Bind(string key, string value)
    {
        CacheReferences();
        if (keyText != null) keyText.SetText(key);
        if (valueText != null) valueText.SetText(value);
    }

    private void CacheReferences()
    {
        if (keyText == null)
        {
            Transform key = transform.Find("Key");
            if (key != null) keyText = key.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (valueText == null)
        {
            Transform value = transform.Find("Value");
            if (value != null) valueText = value.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
