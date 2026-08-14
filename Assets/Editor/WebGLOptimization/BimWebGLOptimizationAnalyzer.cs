using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pixyz.ImportSDK;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BimWebGLOptimization
{
    internal sealed class RepeatedRenderCombination
    {
        public string MeshName { get; set; }
        public string MaterialNames { get; set; }
        public int InstanceCount { get; set; }
    }

    internal sealed class BimWebGLSceneReport
    {
        public string SceneName { get; set; }
        public string ScenePath { get; set; }
        public DateTime GeneratedUtc { get; set; }
        public int GameObjectCount { get; set; }
        public int ActiveObjectCount { get; set; }
        public int RendererCount { get; set; }
        public long TriangleCount { get; set; }
        public int MaterialSlotCount { get; set; }
        public int TransformOnlyLeafCount { get; set; }
        public int TransformOnlyCurve2Count { get; set; }
        public int MeshColliderCount { get; set; }
        public int NonSelectableMeshColliderCount { get; set; }
        public int RemovableVisualMeshColliderCount { get; set; }
        public int BackgroundWallMeshColliderCount { get; set; }
        public int BackgroundWallBoxProxyCount { get; set; }
        public int BackgroundWallInvalidBoxProxyCount { get; set; }
        public int MetadataComponentCount { get; set; }
        public int UnitObjectCount { get; set; }
        public int BackgroundWallObjectCount { get; set; }
        public int SelectableLayerAssignmentCount { get; set; }
        public int BimInspectableLayerAssignmentCount { get; set; }
        public int CameraCollisionLayerAssignmentCount { get; set; }
        public int ReadableMeshCount { get; set; }
        public int ColliderRequiredReadableMeshCount { get; set; }
        public int MeshReadWriteDisableCandidateCount { get; set; }
        public int RepeatedCombinationGroupCount { get; set; }
        public int RepeatedCombinationInstanceCount { get; set; }
        public int ObjectsWithoutStaticFlagsCount { get; set; }
        public int PotentialColliderProxyCount { get; set; }
        public IReadOnlyList<RepeatedRenderCombination> TopRepeatedCombinations { get; set; }
        public IReadOnlyList<string> MeshReadWriteDisableCandidatePaths { get; set; }

        public string ToMarkdown()
        {
            StringBuilder builder = new StringBuilder(2048);
            builder.AppendLine("# BIM WebGL Cleanup Dry Run");
            builder.AppendLine();
            builder.AppendLine($"Generated: {GeneratedUtc:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine();
            builder.AppendLine($"Scene: `{ScenePath}`");
            builder.AppendLine();
            builder.AppendLine("No scene objects or assets were modified by this analysis.");
            builder.AppendLine();
            builder.AppendLine("## Counts");
            builder.AppendLine();
            builder.AppendLine("| Metric | Count |");
            builder.AppendLine("| --- | ---: |");
            builder.AppendLine($"| GameObjects | {GameObjectCount:N0} |");
            builder.AppendLine($"| Active GameObjects | {ActiveObjectCount:N0} |");
            builder.AppendLine($"| Renderers | {RendererCount:N0} |");
            builder.AppendLine($"| Triangles | {TriangleCount:N0} |");
            builder.AppendLine($"| Material slots | {MaterialSlotCount:N0} |");
            builder.AppendLine($"| Transform-only leaves | {TransformOnlyLeafCount:N0} |");
            builder.AppendLine($"| Transform-only `Curve_2` leaves | {TransformOnlyCurve2Count:N0} |");
            builder.AppendLine($"| MeshColliders | {MeshColliderCount:N0} |");
            builder.AppendLine($"| MeshColliders on non-selectable objects | {NonSelectableMeshColliderCount:N0} |");
            builder.AppendLine($"| Purely visual MeshColliders eligible for removal | {RemovableVisualMeshColliderCount:N0} |");
            builder.AppendLine($"| BackgroundWall MeshColliders | {BackgroundWallMeshColliderCount:N0} |");
            builder.AppendLine($"| BackgroundWall MeshColliders eligible for BoxCollider proxies | {BackgroundWallBoxProxyCount:N0} |");
            builder.AppendLine($"| Negative-scale BackgroundWall BoxColliders requiring repair | {BackgroundWallInvalidBoxProxyCount:N0} |");
            builder.AppendLine($"| Pixyz Metadata components | {MetadataComponentCount:N0} |");
            builder.AppendLine($"| Unit-tagged objects | {UnitObjectCount:N0} |");
            builder.AppendLine($"| BackgroundWall-tagged objects | {BackgroundWallObjectCount:N0} |");
            builder.AppendLine($"| Planned Selectable layer assignments | {SelectableLayerAssignmentCount:N0} |");
            builder.AppendLine($"| Planned BimInspectable layer assignments | {BimInspectableLayerAssignmentCount:N0} |");
            builder.AppendLine($"| Planned CameraCollision layer assignments | {CameraCollisionLayerAssignmentCount:N0} |");
            builder.AppendLine($"| Distinct readable meshes | {ReadableMeshCount:N0} |");
            builder.AppendLine($"| Readable meshes still required by MeshCollider | {ColliderRequiredReadableMeshCount:N0} |");
            builder.AppendLine($"| Model assets safe to disable Read/Write | {MeshReadWriteDisableCandidateCount:N0} |");
            builder.AppendLine($"| Repeated mesh/material groups | {RepeatedCombinationGroupCount:N0} |");
            builder.AppendLine($"| Instances in repeated groups | {RepeatedCombinationInstanceCount:N0} |");
            builder.AppendLine($"| Objects without static flags | {ObjectsWithoutStaticFlagsCount:N0} |");
            builder.AppendLine($"| Potential collider proxies | {PotentialColliderProxyCount:N0} |");
            builder.AppendLine();
            builder.AppendLine("## Selection Guardrail");
            builder.AppendLine();
            builder.AppendLine(
                $"{MeshColliderCount - NonSelectableMeshColliderCount:N0} MeshColliders are attached to apartment or BIM inspection targets. "
                + "They remain because runtime metadata selection currently resolves the hit object or its immediate parent; "
                + "grouped proxies require an element-ID mapping before these colliders can be removed safely.");
            builder.AppendLine();
            builder.AppendLine("## Top Repeated Mesh/Material Combinations");
            builder.AppendLine();
            builder.AppendLine("| Mesh | Materials | Instances |");
            builder.AppendLine("| --- | --- | ---: |");

            foreach (RepeatedRenderCombination combination in TopRepeatedCombinations)
            {
                builder.AppendLine($"| {EscapeTableCell(combination.MeshName)} | {EscapeTableCell(combination.MaterialNames)} | {combination.InstanceCount:N0} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Safe Read/Write Disable Candidates");
            builder.AppendLine();
            if (MeshReadWriteDisableCandidatePaths.Count == 0)
            {
                builder.AppendLine("(none)");
            }
            else
            {
                foreach (string path in MeshReadWriteDisableCandidatePaths)
                {
                    builder.AppendLine($"- `{path}`");
                }
            }

            return builder.ToString();
        }

        private static string EscapeTableCell(string value)
        {
            return string.IsNullOrEmpty(value) ? "(none)" : value.Replace("|", "\\|");
        }
    }

    internal static class BimWebGLOptimizationAnalyzer
    {
        internal const string DryRunReportPath = "Docs/WebGLOptimizationDryRun.md";

        private sealed class RenderCombinationAccumulator
        {
            public string MeshName;
            public string MaterialNames;
            public int Count;
        }

        public static BimWebGLSceneReport AnalyzeActiveScene(bool writeReport)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("A loaded active scene is required for WebGL analysis.");
            }

            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            BimWebGLSceneReport report = new BimWebGLSceneReport
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                GeneratedUtc = DateTime.UtcNow
            };

            HashSet<Mesh> sceneMeshes = new HashSet<Mesh>();
            HashSet<Mesh> colliderMeshes = new HashSet<Mesh>();
            Dictionary<string, RenderCombinationAccumulator> renderCombinations =
                new Dictionary<string, RenderCombinationAccumulator>(StringComparer.Ordinal);

            try
            {
                for (int i = 0; i < allTransforms.Length; i++)
                {
                    if ((i & 255) == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "BIM WebGL Analysis",
                            $"Inspecting scene objects ({i:N0}/{allTransforms.Length:N0})",
                            allTransforms.Length == 0 ? 1f : (float)i / allTransforms.Length);
                    }

                    Transform transform = allTransforms[i];
                    GameObject gameObject = transform.gameObject;
                    if (gameObject.scene != scene)
                    {
                        continue;
                    }

                    report.GameObjectCount++;
                    if (gameObject.activeInHierarchy)
                    {
                        report.ActiveObjectCount++;
                    }

                    if (GameObjectUtility.GetStaticEditorFlags(gameObject) == 0)
                    {
                        report.ObjectsWithoutStaticFlagsCount++;
                    }

                    bool isTransformOnlyLeaf = IsTransformOnlyLeaf(transform);
                    bool isRemovableCurve2 = isTransformOnlyLeaf
                        && string.Equals(gameObject.name, "Curve_2", StringComparison.Ordinal);
                    if (isTransformOnlyLeaf)
                    {
                        report.TransformOnlyLeafCount++;
                        if (isRemovableCurve2)
                        {
                            report.TransformOnlyCurve2Count++;
                        }
                    }

                    Metadata metadata = gameObject.GetComponent<Metadata>();
                    bool hasMetadata = metadata != null;
                    bool hasParentMetadata = transform.parent != null
                        && transform.parent.GetComponent<Metadata>() != null;
                    bool isUnit = gameObject.CompareTag("Unit");
                    bool isBackgroundWall = gameObject.CompareTag("BackgroundWall");
                    bool isBimInspectable = hasMetadata || hasParentMetadata;
                    string currentLayerName = LayerMask.LayerToName(gameObject.layer);

                    if (hasMetadata)
                    {
                        report.MetadataComponentCount++;
                    }

                    if (isRemovableCurve2)
                    {
                        continue;
                    }

                    if (isUnit)
                    {
                        report.UnitObjectCount++;
                        if (!string.Equals(currentLayerName, "Selectable", StringComparison.Ordinal))
                        {
                            report.SelectableLayerAssignmentCount++;
                        }
                    }
                    else if (isBackgroundWall)
                    {
                        report.BackgroundWallObjectCount++;
                        if (!string.Equals(currentLayerName, "CameraCollision", StringComparison.Ordinal))
                        {
                            report.CameraCollisionLayerAssignmentCount++;
                        }
                    }
                    else if (isBimInspectable)
                    {
                        if (!string.Equals(currentLayerName, "BimInspectable", StringComparison.Ordinal))
                        {
                            report.BimInspectableLayerAssignmentCount++;
                        }
                    }

                    MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
                    BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
                    if (isBackgroundWall
                        && meshCollider == null
                        && boxCollider != null
                        && HasNegativeLossyScale(transform)
                        && GetSharedMesh(gameObject) != null)
                    {
                        report.BackgroundWallInvalidBoxProxyCount++;
                    }

                    if (meshCollider != null)
                    {
                        report.MeshColliderCount++;
                        if (meshCollider.sharedMesh != null)
                        {
                            colliderMeshes.Add(meshCollider.sharedMesh);
                        }
                        if (!isUnit && !isBimInspectable)
                        {
                            report.NonSelectableMeshColliderCount++;
                        }

                        if (!isUnit && !isBimInspectable && !isBackgroundWall)
                        {
                            report.RemovableVisualMeshColliderCount++;
                        }

                        if (isBackgroundWall)
                        {
                            report.BackgroundWallMeshColliderCount++;
                            if (meshCollider.sharedMesh != null && !HasNegativeLossyScale(transform))
                            {
                                report.BackgroundWallBoxProxyCount++;
                            }
                        }

                        Renderer colliderRenderer = gameObject.GetComponent<Renderer>();
                        if (meshCollider.sharedMesh != null
                            && colliderRenderer != null
                            && meshCollider.sharedMesh.vertexCount > 24
                            && colliderRenderer.bounds.size.sqrMagnitude > 0.0001f)
                        {
                            report.PotentialColliderProxyCount++;
                        }
                    }

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        continue;
                    }

                    report.RendererCount++;
                    Material[] materials = renderer.sharedMaterials;
                    report.MaterialSlotCount += materials.Length;

                    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                    Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                    if (mesh == null)
                    {
                        continue;
                    }

                    sceneMeshes.Add(mesh);
                    report.TriangleCount += GetTriangleCount(mesh);
                    AddRenderCombination(renderCombinations, mesh, materials);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            report.ReadableMeshCount = sceneMeshes.Count(mesh => mesh != null && mesh.isReadable);
            report.ColliderRequiredReadableMeshCount = sceneMeshes.Count(
                mesh => mesh != null && mesh.isReadable && colliderMeshes.Contains(mesh));
            report.MeshReadWriteDisableCandidatePaths = FindSafeReadableMeshImporterPaths(scene);
            report.MeshReadWriteDisableCandidateCount = report.MeshReadWriteDisableCandidatePaths.Count;
            List<RepeatedRenderCombination> repeated = renderCombinations.Values
                .Where(item => item.Count > 1)
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.MeshName, StringComparer.Ordinal)
                .Select(item => new RepeatedRenderCombination
                {
                    MeshName = item.MeshName,
                    MaterialNames = item.MaterialNames,
                    InstanceCount = item.Count
                })
                .ToList();

            report.RepeatedCombinationGroupCount = repeated.Count;
            report.RepeatedCombinationInstanceCount = repeated.Sum(item => item.InstanceCount);
            report.TopRepeatedCombinations = repeated.Take(25).ToArray();

            if (writeReport)
            {
                WriteReport(report);
            }

            return report;
        }

        public static List<GameObject> FindTransformOnlyCurve2Leaves(Scene scene)
        {
            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            List<GameObject> candidates = new List<GameObject>();
            foreach (Transform transform in allTransforms)
            {
                if (transform.gameObject.scene == scene
                    && string.Equals(transform.gameObject.name, "Curve_2", StringComparison.Ordinal)
                    && IsTransformOnlyLeaf(transform))
                {
                    candidates.Add(transform.gameObject);
                }
            }

            return candidates;
        }

        public static IReadOnlyList<string> FindSafeReadableMeshImporterPaths(Scene scene)
        {
            HashSet<Mesh> renderedMeshes = new HashSet<Mesh>();
            HashSet<Mesh> colliderMeshes = new HashSet<Mesh>();

            MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.gameObject.scene == scene && meshFilter.sharedMesh != null)
                {
                    renderedMeshes.Add(meshFilter.sharedMesh);
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                if (renderer.gameObject.scene == scene && renderer.sharedMesh != null)
                {
                    renderedMeshes.Add(renderer.sharedMesh);
                }
            }

            MeshCollider[] meshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MeshCollider meshCollider in meshColliders)
            {
                if (meshCollider.gameObject.scene == scene && meshCollider.sharedMesh != null)
                {
                    colliderMeshes.Add(meshCollider.sharedMesh);
                }
            }

            SortedSet<string> candidatePaths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Mesh mesh in renderedMeshes)
            {
                if (mesh == null || !mesh.isReadable || colliderMeshes.Contains(mesh))
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(mesh);
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null && importer.isReadable)
                {
                    candidatePaths.Add(assetPath);
                }
            }

            return candidatePaths.ToArray();
        }

        public static bool HasNegativeLossyScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            return scale.x < 0f || scale.y < 0f || scale.z < 0f;
        }

        public static Mesh GetSharedMesh(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                return meshFilter.sharedMesh;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            return skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : null;
        }

        private static bool IsTransformOnlyLeaf(Transform transform)
        {
            if (transform.childCount != 0)
            {
                return false;
            }

            Component[] components = transform.GetComponents<Component>();
            return components.Length == 1 && components[0] is Transform;
        }

        private static long GetTriangleCount(Mesh mesh)
        {
            long indexCount = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                indexCount += (long)mesh.GetIndexCount(subMesh);
            }

            return indexCount / 3;
        }

        private static void AddRenderCombination(
            IDictionary<string, RenderCombinationAccumulator> combinations,
            Mesh mesh,
            Material[] materials)
        {
            StringBuilder keyBuilder = new StringBuilder(32 + materials.Length * 12);
            keyBuilder.Append(mesh.GetInstanceID());
            StringBuilder materialNames = new StringBuilder();

            foreach (Material material in materials)
            {
                keyBuilder.Append(':').Append(material != null ? material.GetInstanceID() : 0);
                if (materialNames.Length > 0)
                {
                    materialNames.Append(", ");
                }

                materialNames.Append(material != null ? material.name : "(none)");
            }

            string key = keyBuilder.ToString();
            if (!combinations.TryGetValue(key, out RenderCombinationAccumulator accumulator))
            {
                accumulator = new RenderCombinationAccumulator
                {
                    MeshName = mesh.name,
                    MaterialNames = materialNames.ToString()
                };
                combinations.Add(key, accumulator);
            }

            accumulator.Count++;
        }

        private static void WriteReport(BimWebGLSceneReport report)
        {
            string directory = Path.GetDirectoryName(DryRunReportPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(DryRunReportPath, report.ToMarkdown(), new UTF8Encoding(false));
            Debug.Log($"BIM WebGL dry-run report written to {DryRunReportPath}");
        }
    }
}
