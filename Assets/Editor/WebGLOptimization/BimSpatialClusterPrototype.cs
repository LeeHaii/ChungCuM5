using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BimRuntime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BimWebGLOptimization
{
    internal static class BimSpatialClusterPrototype
    {
        private const string RootPrefix = "__BimSpatialPrototype_";
        private const string AssetRoot = "Assets/Generated/WebGL/SpatialPrototype";
        private const string ReportPath = "Docs/BimSpatialClusterPrototype.md";
        private const float DefaultCellSize = 20f;

        private readonly struct GroupKey : IEquatable<GroupKey>, IComparable<GroupKey>
        {
            public readonly Vector3Int Cell;
            public readonly Material Material;
            private readonly string materialPath;

            public GroupKey(Vector3Int cell, Material material)
            {
                Cell = cell;
                Material = material;
                materialPath = AssetDatabase.GetAssetPath(material) ?? string.Empty;
            }

            public bool Equals(GroupKey other) => Cell == other.Cell && Material == other.Material;
            public override bool Equals(object obj) => obj is GroupKey other && Equals(other);
            public override int GetHashCode() => (Cell.GetHashCode() * 397) ^ (Material != null ? Material.GetInstanceID() : 0);

            public int CompareTo(GroupKey other)
            {
                int comparison = Cell.x.CompareTo(other.Cell.x);
                if (comparison != 0) return comparison;
                comparison = Cell.y.CompareTo(other.Cell.y);
                if (comparison != 0) return comparison;
                comparison = Cell.z.CompareTo(other.Cell.z);
                if (comparison != 0) return comparison;
                comparison = string.CompareOrdinal(materialPath, other.materialPath);
                return comparison != 0
                    ? comparison
                    : string.CompareOrdinal(Material != null ? Material.name : string.Empty, other.Material != null ? other.Material.name : string.Empty);
            }
        }

        private sealed class Fragment
        {
            public Mesh Mesh;
            public int SubMesh;
            public Matrix4x4 Transform;
            public int ElementIndex;
            public MeshRenderer Renderer;
        }

        [MenuItem("Tools/WebGL Optimization/Spatial Prototype/Build From Selected Floor")]
        public static void BuildFromSelection()
        {
            GameObject floorRoot = Selection.activeGameObject;
            if (floorRoot == null || !floorRoot.scene.IsValid())
                throw new InvalidOperationException("Select a loaded floor root before building the spatial prototype.");
            if (GameObject.Find(RootPrefix + floorRoot.name) != null)
                throw new InvalidOperationException("A spatial prototype with this floor name already exists.");

            BimMetadataStore store = UnityEngine.Object.FindFirstObjectByType<BimMetadataStore>(FindObjectsInactive.Include);
            if (store == null || store.Catalog == null)
                throw new InvalidOperationException("Convert and bind the compact BIM metadata catalog first.");

            MeshRenderer[] candidates = floorRoot.GetComponentsInChildren<MeshRenderer>(true);
            Dictionary<GroupKey, List<Fragment>> groups = new Dictionary<GroupKey, List<Fragment>>(256);
            HashSet<Renderer> sourceRenderers = new HashSet<Renderer>();
            HashSet<Collider> sourceColliders = new HashSet<Collider>();
            int skippedUnits = 0;
            int skippedUnreadable = 0;
            int skippedUnmapped = 0;
            int skippedNegativeScale = 0;

            for (int rendererIndex = 0; rendererIndex < candidates.Length; rendererIndex++)
            {
                MeshRenderer renderer = candidates[rendererIndex];
                if (renderer == null || !renderer.enabled || renderer.CompareTag("Unit"))
                {
                    if (renderer != null && renderer.CompareTag("Unit")) skippedUnits++;
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || !mesh.isReadable)
                {
                    skippedUnreadable++;
                    continue;
                }

                if (renderer.transform.localToWorldMatrix.determinant < 0f)
                {
                    skippedNegativeScale++;
                    continue;
                }

                if (!store.TryGetElement(renderer.transform, out BimMetadataElement element))
                {
                    skippedUnmapped++;
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
                if (subMeshCount == 0) continue;

                Vector3 localCenter = floorRoot.transform.InverseTransformPoint(renderer.bounds.center);
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(localCenter.x / DefaultCellSize),
                    Mathf.FloorToInt(localCenter.y / DefaultCellSize),
                    Mathf.FloorToInt(localCenter.z / DefaultCellSize));

                Matrix4x4 matrix = floorRoot.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    Material material = materials[subMesh];
                    if (material == null) continue;
                    GroupKey key = new GroupKey(cell, material);
                    if (!groups.TryGetValue(key, out List<Fragment> fragments))
                    {
                        fragments = new List<Fragment>();
                        groups.Add(key, fragments);
                    }

                    fragments.Add(new Fragment
                    {
                        Mesh = mesh,
                        SubMesh = subMesh,
                        Transform = matrix,
                        ElementIndex = element.ElementIndex,
                        Renderer = renderer
                    });
                }

                sourceRenderers.Add(renderer);
                Collider collider = renderer.GetComponent<Collider>();
                if (collider != null) sourceColliders.Add(collider);
            }

            if (groups.Count == 0)
                throw new InvalidOperationException("The selected floor produced no readable, mapped, non-unit mesh groups.");

            Directory.CreateDirectory(AssetRoot);
            string safeFloorName = Sanitize(floorRoot.name);
            string floorAssetFolder = $"{AssetRoot}/{safeFloorName}";
            Directory.CreateDirectory(floorAssetFolder);

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build BIM Spatial Cluster Prototype");
            GameObject prototypeRoot = new GameObject(RootPrefix + floorRoot.name);
            Undo.RegisterCreatedObjectUndo(prototypeRoot, "Create spatial prototype root");
            prototypeRoot.transform.SetParent(floorRoot.transform, false);
            BimSpatialClusterPrototypeState state = Undo.AddComponent<BimSpatialClusterPrototypeState>(prototypeRoot);
            List<Mesh> generatedMeshes = new List<Mesh>(groups.Count);
            List<GroupKey> orderedKeys = new List<GroupKey>(groups.Keys);
            orderedKeys.Sort();
            int mappedTriangles = 0;

            try
            {
                for (int groupIndex = 0; groupIndex < orderedKeys.Count; groupIndex++)
                {
                    EditorUtility.DisplayProgressBar(
                        "BIM Spatial Prototype",
                        $"Combining cell groups ({groupIndex:N0}/{orderedKeys.Count:N0})",
                        (float)groupIndex / orderedKeys.Count);

                    GroupKey key = orderedKeys[groupIndex];
                    List<Fragment> fragments = groups[key];
                    CombineInstance[] combines = new CombineInstance[fragments.Count];
                    List<int> triangleMapping = new List<int>();

                    for (int i = 0; i < fragments.Count; i++)
                    {
                        Fragment fragment = fragments[i];
                        combines[i] = new CombineInstance
                        {
                            mesh = fragment.Mesh,
                            subMeshIndex = fragment.SubMesh,
                            transform = fragment.Transform
                        };

                        int triangleCount = (int)(fragment.Mesh.GetIndexCount(fragment.SubMesh) / 3);
                        for (int triangle = 0; triangle < triangleCount; triangle++)
                            triangleMapping.Add(fragment.ElementIndex);
                    }

                    Mesh combined = new Mesh
                    {
                        name = $"BimCell_{key.Cell.x}_{key.Cell.y}_{key.Cell.z}_{groupIndex}",
                        indexFormat = IndexFormat.UInt32
                    };
                    combined.CombineMeshes(combines, true, true, false);
                    combined.RecalculateBounds();
                    string meshPath = $"{floorAssetFolder}/{combined.name}.asset";
                    AssetDatabase.CreateAsset(combined, meshPath);
                    generatedMeshes.Add(combined);

                    GameObject chunk = new GameObject(combined.name);
                    Undo.RegisterCreatedObjectUndo(chunk, "Create spatial chunk");
                    chunk.transform.SetParent(prototypeRoot.transform, false);
                    MeshFilter filter = Undo.AddComponent<MeshFilter>(chunk);
                    MeshRenderer renderer = Undo.AddComponent<MeshRenderer>(chunk);
                    MeshCollider collider = Undo.AddComponent<MeshCollider>(chunk);
                    BimCombinedElementMap map = Undo.AddComponent<BimCombinedElementMap>(chunk);
                    filter.sharedMesh = combined;
                    renderer.sharedMaterial = key.Material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    collider.sharedMesh = combined;
                    map.SetTriangleMapForEditor(triangleMapping.ToArray());
                    mappedTriangles += triangleMapping.Count;
                }

                foreach (Renderer renderer in sourceRenderers)
                {
                    Undo.RecordObject(renderer, "Disable source renderer for spatial prototype");
                    renderer.enabled = false;
                }

                foreach (Collider collider in sourceColliders)
                {
                    Undo.RecordObject(collider, "Disable source collider for spatial prototype");
                    collider.enabled = false;
                }

                state.ConfigureForEditor(
                    new List<Renderer>(sourceRenderers).ToArray(),
                    new List<Collider>(sourceColliders).ToArray(),
                    generatedMeshes.ToArray());
                EditorUtility.SetDirty(state);
                EditorSceneManager.MarkSceneDirty(floorRoot.scene);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                WriteReport(
                    floorRoot,
                    candidates.Length,
                    sourceRenderers.Count,
                    sourceColliders.Count,
                    groups.Count,
                    mappedTriangles,
                    skippedUnits,
                    skippedUnreadable,
                    skippedUnmapped,
                    skippedNegativeScale);
                Selection.activeGameObject = prototypeRoot;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                for (int i = 0; i < generatedMeshes.Count; i++)
                {
                    string path = AssetDatabase.GetAssetPath(generatedMeshes[i]);
                    if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
                }
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/WebGL Optimization/Spatial Prototype/Revert Selected Prototype")]
        public static void RevertSelectedPrototype()
        {
            BimSpatialClusterPrototypeState state = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<BimSpatialClusterPrototypeState>(true)
                : null;
            if (state == null) throw new InvalidOperationException("Select a generated spatial prototype or one of its chunks.");

            Renderer[] sourceRenderers = state.SourceRenderers;
            Collider[] sourceColliders = state.SourceColliders;
            Mesh[] meshes = state.GeneratedMeshes;
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (sourceRenderers[i] == null) continue;
                Undo.RecordObject(sourceRenderers[i], "Restore source renderer");
                sourceRenderers[i].enabled = true;
            }
            for (int i = 0; i < sourceColliders.Length; i++)
            {
                if (sourceColliders[i] == null) continue;
                Undo.RecordObject(sourceColliders[i], "Restore source collider");
                sourceColliders[i].enabled = true;
            }

            Scene scene = state.gameObject.scene;
            Undo.DestroyObjectImmediate(state.gameObject);
            for (int i = 0; i < meshes.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(meshes[i]);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
        }

        private static string Sanitize(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-' ? character : '_');
            }
            return builder.ToString();
        }

        private static void WriteReport(
            GameObject floorRoot,
            int candidateRenderers,
            int disabledRenderers,
            int disabledColliders,
            int generatedChunks,
            int mappedTriangles,
            int skippedUnits,
            int skippedUnreadable,
            int skippedUnmapped,
            int skippedNegativeScale)
        {
            StringBuilder report = new StringBuilder(1024);
            report.AppendLine("# BIM Spatial Cluster Prototype");
            report.AppendLine();
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();
            report.AppendLine($"Selected floor root: `{floorRoot.name}`");
            report.AppendLine();
            report.AppendLine("| Metric | Count |");
            report.AppendLine("| --- | ---: |");
            report.AppendLine($"| Candidate MeshRenderers | {candidateRenderers:N0} |");
            report.AppendLine($"| Clustered source renderers | {disabledRenderers:N0} |");
            report.AppendLine($"| Disabled source colliders | {disabledColliders:N0} |");
            report.AppendLine($"| Generated material/cell chunks | {generatedChunks:N0} |");
            report.AppendLine($"| Triangle-to-element mappings | {mappedTriangles:N0} |");
            report.AppendLine($"| Preserved unit renderers | {skippedUnits:N0} |");
            report.AppendLine($"| Skipped unreadable meshes | {skippedUnreadable:N0} |");
            report.AppendLine($"| Skipped unmapped renderers | {skippedUnmapped:N0} |");
            report.AppendLine($"| Skipped negative-scale renderers | {skippedNegativeScale:N0} |");
            report.AppendLine();
            report.AppendLine("This is a reversible one-floor prototype. Keep it only after the WebGL p95/peak-memory gate improves by at least 15% and interaction coverage passes.");
            File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
        }
    }
}
