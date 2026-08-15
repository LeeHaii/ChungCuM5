using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BimRuntime;
using Pixyz.ImportSDK;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BimWebGLOptimization
{
    internal sealed class BimMetadataConversionReport
    {
        public string ScenePath;
        public int SourceElementCount;
        public int SourcePropertyCount;
        public int CatalogStringCount;
        public int CatalogPropertyCount;
        public int CatalogElementCount;
        public int ValidatedElementCount;
        public int ValidatedPropertyCount;
        public bool Applied;

        public string ToMarkdown()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine("# BIM Metadata Catalog Conversion");
            builder.AppendLine();
            builder.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine();
            builder.AppendLine($"Scene: `{ScenePath}`");
            builder.AppendLine();
            builder.AppendLine(Applied
                ? "The validated catalog was applied and source Pixyz Metadata components were removed."
                : "Dry run only. No scene objects or assets were modified.");
            builder.AppendLine();
            builder.AppendLine("| Metric | Count |");
            builder.AppendLine("| --- | ---: |");
            builder.AppendLine($"| Source elements | {SourceElementCount:N0} |");
            builder.AppendLine($"| Source properties | {SourcePropertyCount:N0} |");
            builder.AppendLine($"| Deduplicated strings | {CatalogStringCount:N0} |");
            builder.AppendLine($"| Catalog elements | {CatalogElementCount:N0} |");
            builder.AppendLine($"| Catalog properties | {CatalogPropertyCount:N0} |");
            builder.AppendLine($"| Validated elements | {ValidatedElementCount:N0} |");
            builder.AppendLine($"| Validated properties | {ValidatedPropertyCount:N0} |");
            builder.AppendLine();
            builder.AppendLine("Validation compares every source property in original array order, including duplicate keys.");
            return builder.ToString();
        }
    }

    internal static class BimMetadataCatalogConverter
    {
        internal const string CatalogAssetPath = "Assets/Generated/WebGL/BimMetadataCatalog.asset";
        internal const string ReportPath = "Docs/BimMetadataConversionReport.md";
        private const string StoreObjectName = "__BimMetadataStore";

        private sealed class SourceElement
        {
            public Metadata Metadata;
            public Transform Transform;
            public string StableId;
            public string[] Keys;
            public string[] Values;
        }

        private sealed class CatalogBuild
        {
            public string[] Strings;
            public BimPropertyRecord[] Properties;
            public BimElementRecord[] Elements;
            public Transform[] Targets;
        }

        [MenuItem("Tools/WebGL Optimization/Metadata/Dry Run and Validate Catalog")]
        public static void DryRunActiveScene()
        {
            Scene scene = RequireOptimizedScene();
            List<SourceElement> source = ReadSource(scene);
            CatalogBuild build = BuildCatalog(source);
            BimMetadataCatalog transientCatalog = ScriptableObject.CreateInstance<BimMetadataCatalog>();

            try
            {
                SetCatalogData(transientCatalog, scene, build);
                BimMetadataConversionReport report = Validate(source, transientCatalog, scene.path, false);
                WriteReport(report);
                Debug.Log(
                    $"BIM metadata catalog dry run passed: {report.ValidatedElementCount:N0} elements and "
                    + $"{report.ValidatedPropertyCount:N0} properties matched exactly.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(transientCatalog);
            }
        }

        [MenuItem("Tools/WebGL Optimization/Metadata/Convert Validated Pixyz Metadata")]
        public static void ConvertActiveScene()
        {
            Scene scene = RequireOptimizedScene();
            List<SourceElement> source = ReadSource(scene);
            if (source.Count == 0)
            {
                throw new InvalidOperationException("The active scene contains no Pixyz Metadata components to convert.");
            }

            CatalogBuild build = BuildCatalog(source);
            BimMetadataCatalog validationCatalog = ScriptableObject.CreateInstance<BimMetadataCatalog>();
            BimMetadataConversionReport report;

            try
            {
                SetCatalogData(validationCatalog, scene, build);
                report = Validate(source, validationCatalog, scene.path, false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validationCatalog);
            }

            EnsureAssetFolder();
            BimMetadataCatalog catalog = AssetDatabase.LoadAssetAtPath<BimMetadataCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BimMetadataCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Convert Pixyz Metadata to Compact BIM Catalog");

            try
            {
                Undo.RecordObject(catalog, "Update BIM metadata catalog");
                SetCatalogData(catalog, scene, build);
                EditorUtility.SetDirty(catalog);

                BimMetadataStore store = FindStore(scene);
                if (store == null)
                {
                    GameObject storeObject = new GameObject(StoreObjectName);
                    Undo.RegisterCreatedObjectUndo(storeObject, "Create BIM metadata store");
                    SceneManager.MoveGameObjectToScene(storeObject, scene);
                    store = Undo.AddComponent<BimMetadataStore>(storeObject);
                }

                Undo.RecordObject(store, "Bind BIM metadata catalog");
                store.ConfigureForEditor(catalog, build.Targets);
                EditorUtility.SetDirty(store);

                report = Validate(source, catalog, scene.path, true);
                for (int i = 0; i < source.Count; i++)
                {
                    if ((i & 127) == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "BIM Metadata Conversion",
                            $"Removing validated Pixyz components ({i:N0}/{source.Count:N0})",
                            source.Count == 0 ? 1f : (float)i / source.Count);
                    }

                    if (source[i].Metadata != null) Undo.DestroyObjectImmediate(source[i].Metadata);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException($"Unity could not save {scene.path} after metadata conversion.");
                }

                Undo.CollapseUndoOperations(undoGroup);
                WriteReport(report);
                Debug.Log(
                    $"Converted and validated {report.ValidatedElementCount:N0} BIM elements with "
                    + $"{report.ValidatedPropertyCount:N0} properties; removed all source Pixyz Metadata components.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static int CountPixyzMetadata(Scene scene)
        {
            Metadata[] all = UnityEngine.Object.FindObjectsByType<Metadata>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.scene == scene) count++;
            }

            return count;
        }

        private static Scene RequireOptimizedScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()
                || !scene.isLoaded
                || !scene.path.EndsWith("_Optimized.unity", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Load an _Optimized.unity scene before converting BIM metadata.");
            }

            return scene;
        }

        private static List<SourceElement> ReadSource(Scene scene)
        {
            Metadata[] all = UnityEngine.Object.FindObjectsByType<Metadata>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            List<SourceElement> source = new List<SourceElement>(all.Length);
            HashSet<Transform> uniqueTargets = new HashSet<Transform>();

            for (int i = 0; i < all.Length; i++)
            {
                Metadata metadata = all[i];
                if (metadata == null || metadata.gameObject.scene != scene) continue;
                if (!uniqueTargets.Add(metadata.transform))
                {
                    throw new InvalidOperationException(
                        $"Multiple Pixyz Metadata components are bound to {GetHierarchyPath(metadata.transform)}.");
                }

                Properties nativeProperties = metadata.getPropertiesNative();
                string[] keys = nativeProperties?.names ?? Array.Empty<string>();
                string[] values = nativeProperties?.values ?? Array.Empty<string>();
                if (keys.Length != values.Length)
                {
                    throw new InvalidOperationException(
                        $"Pixyz metadata key/value lengths differ on {GetHierarchyPath(metadata.transform)}.");
                }

                source.Add(new SourceElement
                {
                    Metadata = metadata,
                    Transform = metadata.transform,
                    StableId = GlobalObjectId.GetGlobalObjectIdSlow(metadata.gameObject).ToString(),
                    Keys = keys,
                    Values = values
                });
            }

            source.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            return source;
        }

        private static CatalogBuild BuildCatalog(IReadOnlyList<SourceElement> source)
        {
            int propertyCapacity = 0;
            for (int i = 0; i < source.Count; i++) propertyCapacity += source[i].Keys.Length;

            Dictionary<string, int> stringLookup = new Dictionary<string, int>(propertyCapacity * 2 + source.Count * 2);
            List<string> strings = new List<string>(propertyCapacity * 2 + source.Count * 2);
            List<BimPropertyRecord> properties = new List<BimPropertyRecord>(propertyCapacity);
            BimElementRecord[] elements = new BimElementRecord[source.Count];
            Transform[] targets = new Transform[source.Count];

            int Intern(string value)
            {
                value = value ?? string.Empty;
                if (stringLookup.TryGetValue(value, out int existing)) return existing;
                int index = strings.Count;
                strings.Add(value);
                stringLookup.Add(value, index);
                return index;
            }

            for (int elementIndex = 0; elementIndex < source.Count; elementIndex++)
            {
                SourceElement item = source[elementIndex];
                int propertyOffset = properties.Count;
                for (int propertyIndex = 0; propertyIndex < item.Keys.Length; propertyIndex++)
                {
                    properties.Add(new BimPropertyRecord(
                        Intern(item.Keys[propertyIndex]),
                        Intern(item.Values[propertyIndex])));
                }

                elements[elementIndex] = new BimElementRecord(
                    Intern(item.StableId),
                    Intern(item.Transform.name),
                    propertyOffset,
                    item.Keys.Length);
                targets[elementIndex] = item.Transform;
            }

            return new CatalogBuild
            {
                Strings = strings.ToArray(),
                Properties = properties.ToArray(),
                Elements = elements,
                Targets = targets
            };
        }

        private static void SetCatalogData(BimMetadataCatalog catalog, Scene scene, CatalogBuild build)
        {
            catalog.SetDataForEditor(
                AssetDatabase.AssetPathToGUID(scene.path),
                scene.path,
                build.Strings,
                build.Properties,
                build.Elements);
        }

        private static BimMetadataConversionReport Validate(
            IReadOnlyList<SourceElement> source,
            BimMetadataCatalog catalog,
            string scenePath,
            bool applied)
        {
            if (catalog.ElementCount != source.Count)
            {
                throw new InvalidOperationException(
                    $"Catalog element count {catalog.ElementCount:N0} does not match source count {source.Count:N0}.");
            }

            int propertyCount = 0;
            for (int elementIndex = 0; elementIndex < source.Count; elementIndex++)
            {
                SourceElement item = source[elementIndex];
                if (!string.Equals(catalog.GetStableId(elementIndex), item.StableId, StringComparison.Ordinal)
                    || !string.Equals(catalog.GetDisplayName(elementIndex), item.Transform.name, StringComparison.Ordinal)
                    || catalog.GetPropertyCount(elementIndex) != item.Keys.Length)
                {
                    throw new InvalidOperationException($"Catalog element mismatch at index {elementIndex:N0}.");
                }

                for (int propertyIndex = 0; propertyIndex < item.Keys.Length; propertyIndex++)
                {
                    if (!catalog.TryGetProperty(elementIndex, propertyIndex, out string key, out string value)
                        || !string.Equals(key, item.Keys[propertyIndex] ?? string.Empty, StringComparison.Ordinal)
                        || !string.Equals(value, item.Values[propertyIndex] ?? string.Empty, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Catalog property mismatch at element {elementIndex:N0}, property {propertyIndex:N0}.");
                    }

                    propertyCount++;
                }
            }

            return new BimMetadataConversionReport
            {
                ScenePath = scenePath,
                SourceElementCount = source.Count,
                SourcePropertyCount = propertyCount,
                CatalogStringCount = catalog.StringCount,
                CatalogPropertyCount = catalog.PropertyCount,
                CatalogElementCount = catalog.ElementCount,
                ValidatedElementCount = source.Count,
                ValidatedPropertyCount = propertyCount,
                Applied = applied
            };
        }

        private static BimMetadataStore FindStore(Scene scene)
        {
            BimMetadataStore[] stores = UnityEngine.Object.FindObjectsByType<BimMetadataStore>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            BimMetadataStore result = null;
            for (int i = 0; i < stores.Length; i++)
            {
                if (stores[i] == null || stores[i].gameObject.scene != scene) continue;
                if (result != null)
                    throw new InvalidOperationException("The optimized scene contains multiple BIM metadata stores.");
                result = stores[i];
            }

            return result;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            StringBuilder builder = new StringBuilder(transform.name);
            Transform parent = transform.parent;
            while (parent != null)
            {
                builder.Insert(0, '/').Insert(0, parent.name);
                parent = parent.parent;
            }

            return builder.ToString();
        }

        private static void EnsureAssetFolder()
        {
            string directory = Path.GetDirectoryName(CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        }

        private static void WriteReport(BimMetadataConversionReport report)
        {
            string directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(ReportPath, report.ToMarkdown(), new UTF8Encoding(false));
        }
    }
}
