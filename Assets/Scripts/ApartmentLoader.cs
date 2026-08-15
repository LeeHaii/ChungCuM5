using UnityEngine;
#if !UNITY_WEBGL
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class ApartmentLoader : MonoBehaviour
{
#if !UNITY_WEBGL
    void Start()
    {
        string addressableStr = PlayerPrefs.GetString("SelectedApartment", "");
        if (!string.IsNullOrEmpty(addressableStr))
        {
            LoadApartment(addressableStr);
        }
        else
        {
            Debug.LogError("No addressable string found in PlayerPrefs.");
        }
    }

    private void LoadApartment(string addressableKey)
    {
        Addressables.InstantiateAsync(addressableKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully loaded apartment: {addressableKey}");
            }
            else
            {
                Debug.LogError($"Failed to load apartment: {addressableKey}");
            }
        };
    }
#else
    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("Apartment addressable loading is disabled in the Web player.", this);
#endif
    }
#endif
}
