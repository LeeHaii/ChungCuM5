using System;
using UnityEngine;

namespace BimRuntime
{
    [DisallowMultipleComponent]
    public sealed class BimSpatialClusterPrototypeState : MonoBehaviour
    {
        [SerializeField] private Renderer[] sourceRenderers = Array.Empty<Renderer>();
        [SerializeField] private Collider[] sourceColliders = Array.Empty<Collider>();
        [SerializeField] private Mesh[] generatedMeshes = Array.Empty<Mesh>();

        public Renderer[] SourceRenderers => sourceRenderers;
        public Collider[] SourceColliders => sourceColliders;
        public Mesh[] GeneratedMeshes => generatedMeshes;

#if UNITY_EDITOR
        public void ConfigureForEditor(Renderer[] renderers, Collider[] colliders, Mesh[] meshes)
        {
            sourceRenderers = renderers ?? Array.Empty<Renderer>();
            sourceColliders = colliders ?? Array.Empty<Collider>();
            generatedMeshes = meshes ?? Array.Empty<Mesh>();
        }
#endif
    }
}
