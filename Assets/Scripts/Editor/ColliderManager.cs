using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

// The marker component has been removed per user request.

public class ColliderManager : MonoBehaviour
{
    // Allows you to run this from the component's context menu in the Inspector
    [ContextMenu("Add Mesh Colliders")]
    public void AddMeshColliders()
    {
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>(false);
        int addedCount = 0;
        int modifiedCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            GameObject go = renderer.gameObject;
            
            if (!go.activeInHierarchy)
                continue;

            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = go.AddComponent<MeshCollider>();
                collider.convex = false;
                addedCount++;
            }
            else if (collider.convex)
            {
                collider.convex = false;
                modifiedCount++;
            }
        }

        Debug.Log($"Added {addedCount} new MeshColliders and set {modifiedCount} existing ones to non-convex.");
    }

    void Start()
    {
        // Uncomment the line below if you want this to happen automatically when the game starts.
        // NOTE: Doing this at runtime can be performance intensive for large scenes!
        // AddMeshColliders();
    }

#if UNITY_EDITOR
    // Adds a top menu item in the Unity Editor for convenience
    [MenuItem("Tools/Colliders/Add Mesh Colliders to All Active Renderers")]
    public static void AddCollidersMenu()
    {
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>(false);
        int addedCount = 0;
        int modifiedCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            GameObject go = renderer.gameObject;
            
            if (!go.activeInHierarchy)
                continue;
                
            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
            {
                Undo.AddComponent<MeshCollider>(go);
                collider = go.GetComponent<MeshCollider>();
                collider.convex = false;
                addedCount++;
            }
            else if (collider.convex)
            {
                Undo.RecordObject(collider, "Set Non-Convex");
                collider.convex = false;
                modifiedCount++;
            }
        }

        Debug.Log($"[Editor] Added {addedCount} new MeshColliders and set {modifiedCount} existing ones to non-convex.");
    }
#endif
}
