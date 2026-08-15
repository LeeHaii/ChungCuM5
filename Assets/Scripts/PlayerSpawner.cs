using UnityEngine;
#if !UNITY_WEBGL
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("The Addressable key/address for the Player prefab")]
    public string playerAddress = "Player";

    private void Start()
    {
#if !UNITY_WEBGL
        if (!string.IsNullOrEmpty(playerAddress))
        {
            // Spawn the player using Addressables string key
            Addressables.InstantiateAsync(playerAddress, transform.position, transform.rotation).Completed += (handle) =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"PlayerSpawner: Failed to instantiate Addressable with key '{playerAddress}'.");
                }
            };
        }
        else
        {
            Debug.LogError("PlayerSpawner: Player Address string is empty.");
        }
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("Addressable player spawning is disabled in the Web player.", this);
#endif
    }
}
