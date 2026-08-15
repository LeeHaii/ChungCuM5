#if UNITY_INCLUDE_TESTS
using BimRuntime;
using NUnit.Framework;
using UnityEngine;

namespace BimWebGLOptimization.Tests
{
    public sealed class BimMetadataCatalogTests
    {
        [Test]
        public void Catalog_PreservesPropertyOrderAndDuplicateKeys()
        {
            BimMetadataCatalog catalog = ScriptableObject.CreateInstance<BimMetadataCatalog>();
            try
            {
                catalog.SetDataForEditor(
                    "scene-guid",
                    "Assets/Scenes/Test.unity",
                    new[] { "id-1", "Wall", "Key", "First", "Second" },
                    new[] { new BimPropertyRecord(2, 3), new BimPropertyRecord(2, 4) },
                    new[] { new BimElementRecord(0, 1, 0, 2) });

                Assert.That(catalog.ElementCount, Is.EqualTo(1));
                Assert.That(catalog.GetPropertyCount(0), Is.EqualTo(2));
                Assert.That(catalog.TryGetProperty(0, 0, out string firstKey, out string firstValue), Is.True);
                Assert.That(catalog.TryGetProperty(0, 1, out string secondKey, out string secondValue), Is.True);
                Assert.That(firstKey, Is.EqualTo("Key"));
                Assert.That(secondKey, Is.EqualTo("Key"));
                Assert.That(firstValue, Is.EqualTo("First"));
                Assert.That(secondValue, Is.EqualTo("Second"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Store_ResolvesBoundTransformAndParentPathWithoutAllocationApi()
        {
            BimMetadataCatalog catalog = ScriptableObject.CreateInstance<BimMetadataCatalog>();
            GameObject root = new GameObject("Element");
            GameObject child = new GameObject("Collider");
            child.transform.SetParent(root.transform);
            BimMetadataStore store = root.AddComponent<BimMetadataStore>();

            try
            {
                catalog.SetDataForEditor(
                    "scene-guid",
                    "Assets/Scenes/Test.unity",
                    new[] { "id-1", "Element" },
                    System.Array.Empty<BimPropertyRecord>(),
                    new[] { new BimElementRecord(0, 1, 0, 0) });
                store.ConfigureForEditor(catalog, new[] { root.transform });

                Assert.That(store.TryGetElement(child.transform, out BimMetadataElement element), Is.True);
                Assert.That(element.ElementIndex, Is.EqualTo(0));
                Assert.That(element.Target, Is.EqualTo(root.transform));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CombinedMap_RejectsOutOfRangeTriangles()
        {
            GameObject gameObject = new GameObject("Combined");
            try
            {
                BimCombinedElementMap map = gameObject.AddComponent<BimCombinedElementMap>();
                map.SetTriangleMapForEditor(new[] { 4, 4, 9 });

                Assert.That(map.TryGetElementIndex(2, out int elementIndex), Is.True);
                Assert.That(elementIndex, Is.EqualTo(9));
                Assert.That(map.TryGetElementIndex(3, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
#endif
