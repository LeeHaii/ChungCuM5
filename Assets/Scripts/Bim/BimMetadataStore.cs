using System;
using System.Collections.Generic;
using UnityEngine;

namespace BimRuntime
{
    public interface IBimMetadataStore
    {
        int ElementCount { get; }
        bool TryGetElement(Transform source, out BimMetadataElement element);
        bool TryGetElement(RaycastHit hit, out BimMetadataElement element);
    }

    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class BimMetadataStore : MonoBehaviour, IBimMetadataStore
    {
        [SerializeField] private BimMetadataCatalog catalog;
        [SerializeField] private Transform[] elementTargets = Array.Empty<Transform>();

        private Dictionary<Transform, int> elementLookup;

        public static BimMetadataStore Instance { get; private set; }
        public int ElementCount => catalog != null ? catalog.ElementCount : 0;
        public BimMetadataCatalog Catalog => catalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple BimMetadataStore components are loaded. Only one catalog can be active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            BuildLookup();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool TryGetElement(Transform source, out BimMetadataElement element)
        {
            EnsureLookup();

            Transform current = source;
            for (int depth = 0; current != null && depth < 3; depth++, current = current.parent)
            {
                if (elementLookup != null && elementLookup.TryGetValue(current, out int elementIndex))
                {
                    return TryCreateElement(elementIndex, out element);
                }
            }

            element = default;
            return false;
        }

        public bool TryGetElement(RaycastHit hit, out BimMetadataElement element)
        {
            if (hit.collider == null)
            {
                element = default;
                return false;
            }

            BimCombinedElementMap combinedMap = hit.collider.GetComponent<BimCombinedElementMap>();
            if (combinedMap != null
                && combinedMap.TryGetElementIndex(hit.triangleIndex, out int combinedElementIndex)
                && TryCreateElement(combinedElementIndex, out element))
            {
                return true;
            }

            return TryGetElement(hit.collider.transform, out element);
        }

        public bool TryGetElementByIndex(int elementIndex, out BimMetadataElement element)
        {
            return TryCreateElement(elementIndex, out element);
        }

        private bool TryCreateElement(int elementIndex, out BimMetadataElement element)
        {
            if (catalog == null
                || elementTargets == null
                || !catalog.IsValidElementIndex(elementIndex)
                || (uint)elementIndex >= (uint)elementTargets.Length
                || elementTargets[elementIndex] == null)
            {
                element = default;
                return false;
            }

            element = new BimMetadataElement(catalog, elementIndex, elementTargets[elementIndex]);
            return true;
        }

        private void EnsureLookup()
        {
            if (elementLookup == null)
            {
                BuildLookup();
            }
        }

        private void BuildLookup()
        {
            int capacity = elementTargets?.Length ?? 0;
            elementLookup = new Dictionary<Transform, int>(capacity);

            if (catalog == null || elementTargets == null || catalog.ElementCount != elementTargets.Length)
            {
                Debug.LogError("BIM metadata catalog and scene bindings are missing or have different lengths.", this);
                return;
            }

            for (int i = 0; i < elementTargets.Length; i++)
            {
                Transform target = elementTargets[i];
                if (target != null && !elementLookup.ContainsKey(target))
                {
                    elementLookup.Add(target, i);
                }
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(BimMetadataCatalog newCatalog, Transform[] targets)
        {
            catalog = newCatalog;
            elementTargets = targets ?? Array.Empty<Transform>();
            elementLookup = null;
        }

        public bool TryGetElementIndexForEditor(Transform target, out int elementIndex)
        {
            EnsureLookup();
            elementIndex = -1;
            return target != null && elementLookup.TryGetValue(target, out elementIndex);
        }
#endif
    }
}
