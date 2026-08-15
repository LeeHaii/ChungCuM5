using System;
using UnityEngine;

namespace BimRuntime
{
    [Serializable]
    public struct BimPropertyRecord
    {
        [SerializeField] private int keyIndex;
        [SerializeField] private int valueIndex;

        public int KeyIndex => keyIndex;
        public int ValueIndex => valueIndex;

        public BimPropertyRecord(int keyIndex, int valueIndex)
        {
            this.keyIndex = keyIndex;
            this.valueIndex = valueIndex;
        }
    }

    [Serializable]
    public struct BimElementRecord
    {
        [SerializeField] private int stableIdIndex;
        [SerializeField] private int displayNameIndex;
        [SerializeField] private int propertyOffset;
        [SerializeField] private int propertyCount;

        public int StableIdIndex => stableIdIndex;
        public int DisplayNameIndex => displayNameIndex;
        public int PropertyOffset => propertyOffset;
        public int PropertyCount => propertyCount;

        public BimElementRecord(int stableIdIndex, int displayNameIndex, int propertyOffset, int propertyCount)
        {
            this.stableIdIndex = stableIdIndex;
            this.displayNameIndex = displayNameIndex;
            this.propertyOffset = propertyOffset;
            this.propertyCount = propertyCount;
        }
    }

    [CreateAssetMenu(fileName = "BimMetadataCatalog", menuName = "BIM/Metadata Catalog")]
    public sealed class BimMetadataCatalog : ScriptableObject
    {
        [SerializeField] private string sourceSceneGuid;
        [SerializeField] private string sourceScenePath;
        [SerializeField] private string[] stringTable = Array.Empty<string>();
        [SerializeField] private BimPropertyRecord[] properties = Array.Empty<BimPropertyRecord>();
        [SerializeField] private BimElementRecord[] elements = Array.Empty<BimElementRecord>();

        public string SourceSceneGuid => sourceSceneGuid;
        public string SourceScenePath => sourceScenePath;
        public int StringCount => stringTable?.Length ?? 0;
        public int PropertyCount => properties?.Length ?? 0;
        public int ElementCount => elements?.Length ?? 0;

        public bool IsValidElementIndex(int elementIndex)
        {
            return elements != null && (uint)elementIndex < (uint)elements.Length;
        }

        public string GetStableId(int elementIndex)
        {
            return TryGetElementRecord(elementIndex, out BimElementRecord record)
                ? GetString(record.StableIdIndex)
                : string.Empty;
        }

        public string GetDisplayName(int elementIndex)
        {
            return TryGetElementRecord(elementIndex, out BimElementRecord record)
                ? GetString(record.DisplayNameIndex)
                : string.Empty;
        }

        public int GetPropertyCount(int elementIndex)
        {
            return TryGetElementRecord(elementIndex, out BimElementRecord record) ? record.PropertyCount : 0;
        }

        public bool TryGetProperty(int elementIndex, int propertyIndex, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            if (!TryGetElementRecord(elementIndex, out BimElementRecord element)
                || (uint)propertyIndex >= (uint)element.PropertyCount)
            {
                return false;
            }

            int flatIndex = element.PropertyOffset + propertyIndex;
            if (properties == null || (uint)flatIndex >= (uint)properties.Length)
            {
                return false;
            }

            BimPropertyRecord property = properties[flatIndex];
            key = GetString(property.KeyIndex);
            value = GetString(property.ValueIndex);
            return true;
        }

        private bool TryGetElementRecord(int elementIndex, out BimElementRecord record)
        {
            if (!IsValidElementIndex(elementIndex))
            {
                record = default;
                return false;
            }

            record = elements[elementIndex];
            return true;
        }

        private string GetString(int index)
        {
            return stringTable != null && (uint)index < (uint)stringTable.Length
                ? stringTable[index] ?? string.Empty
                : string.Empty;
        }

#if UNITY_EDITOR
        public void SetDataForEditor(
            string sceneGuid,
            string scenePath,
            string[] strings,
            BimPropertyRecord[] propertyRecords,
            BimElementRecord[] elementRecords)
        {
            sourceSceneGuid = sceneGuid ?? string.Empty;
            sourceScenePath = scenePath ?? string.Empty;
            stringTable = strings ?? Array.Empty<string>();
            properties = propertyRecords ?? Array.Empty<BimPropertyRecord>();
            elements = elementRecords ?? Array.Empty<BimElementRecord>();
        }
#endif
    }

    public readonly struct BimMetadataElement
    {
        private readonly BimMetadataCatalog catalog;

        public int ElementIndex { get; }
        public Transform Target { get; }
        public bool IsValid => catalog != null && Target != null && catalog.IsValidElementIndex(ElementIndex);
        public string StableId => catalog != null ? catalog.GetStableId(ElementIndex) : string.Empty;
        public string DisplayName => catalog != null ? catalog.GetDisplayName(ElementIndex) : string.Empty;
        public int PropertyCount => catalog != null ? catalog.GetPropertyCount(ElementIndex) : 0;

        public BimMetadataElement(BimMetadataCatalog catalog, int elementIndex, Transform target)
        {
            this.catalog = catalog;
            ElementIndex = elementIndex;
            Target = target;
        }

        public bool TryGetProperty(int propertyIndex, out string key, out string value)
        {
            if (catalog != null)
            {
                return catalog.TryGetProperty(ElementIndex, propertyIndex, out key, out value);
            }

            key = string.Empty;
            value = string.Empty;
            return false;
        }
    }
}
