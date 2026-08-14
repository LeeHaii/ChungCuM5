using System;
using System.Collections.Generic;
using Pixyz.ImportSDK;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BimWebGLOptimization
{
    internal sealed class BimWebGLCleanupSummary
    {
        public int RemovedCurve2Leaves { get; set; }
        public int AssignedSelectableLayer { get; set; }
        public int AssignedBimInspectableLayer { get; set; }
        public int AssignedCameraCollisionLayer { get; set; }
        public int RemovedVisualMeshColliders { get; set; }
        public int ConvertedCameraCollisionProxies { get; set; }
        public int RestoredNegativeScaleMeshColliders { get; set; }
        public int DisabledMeshReadWriteImporters { get; set; }
    }

    internal static class BimWebGLMilestone4Cleanup
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string SelectableLayerName = "Selectable";
        private const string BimInspectableLayerName = "BimInspectable";
        private const string CameraCollisionLayerName = "CameraCollision";

        public static BimWebGLCleanupSummary Apply(Scene scene, BimWebGLSceneReport approvedReport)
        {
            if (!scene.IsValid() || !scene.isLoaded || approvedReport == null || approvedReport.ScenePath != scene.path)
            {
                throw new InvalidOperationException("A dry-run report for the active scene is required before cleanup.");
            }

            List<GameObject> curve2Leaves = BimWebGLOptimizationAnalyzer.FindTransformOnlyCurve2Leaves(scene);
            if (curve2Leaves.Count != approvedReport.TransformOnlyCurve2Count)
            {
                throw new InvalidOperationException(
                    $"Scene changed after dry run. Expected {approvedReport.TransformOnlyCurve2Count:N0} Curve_2 leaves "
                    + $"but found {curve2Leaves.Count:N0}.");
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply BIM WebGL Hierarchy and Collision Cleanup");

            BimWebGLCleanupSummary summary = new BimWebGLCleanupSummary();
            List<string> disabledMeshReadWritePaths = new List<string>();

            try
            {
                (int selectableLayer, int bimInspectableLayer, int cameraCollisionLayer) = EnsureProjectLayers();
                RemoveCurve2Leaves(curve2Leaves, summary);
                ApplyLayersAndColliders(
                    scene,
                    selectableLayer,
                    bimInspectableLayer,
                    cameraCollisionLayer,
                    summary);
                ConfigureRuntimeRaycastMasks(scene, selectableLayer, bimInspectableLayer, cameraCollisionLayer);
                DisableSafeMeshReadWrite(scene, approvedReport, disabledMeshReadWritePaths, summary);
                ValidateSummary(approvedReport, summary);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException($"Unity could not save {scene.path} after cleanup.");
                }

                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                return summary;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                RestoreMeshReadWrite(disabledMeshReadWritePaths);
                AssetDatabase.SaveAssets();
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static (int selectable, int bimInspectable, int cameraCollision) EnsureProjectLayers()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets.Length == 0 || assets[0] == null)
            {
                throw new InvalidOperationException("Unity TagManager settings could not be loaded.");
            }

            UnityEngine.Object tagManager = assets[0];
            SerializedObject serializedTagManager = new SerializedObject(tagManager);
            SerializedProperty layers = serializedTagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                throw new InvalidOperationException("Unity TagManager layer array could not be found.");
            }

            Undo.RecordObject(tagManager, "Add BIM WebGL Layers");
            int selectable = EnsureLayer(layers, SelectableLayerName);
            int bimInspectable = EnsureLayer(layers, BimInspectableLayerName);
            int cameraCollision = EnsureLayer(layers, CameraCollisionLayerName);
            serializedTagManager.ApplyModifiedPropertiesWithoutUndo();

            return (selectable, bimInspectable, cameraCollision);
        }

        private static int EnsureLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 6; i < layers.arraySize; i++)
            {
                if (string.Equals(layers.GetArrayElementAtIndex(i).stringValue, layerName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            for (int i = 6; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    return i;
                }
            }

            throw new InvalidOperationException($"No free Unity layer slot is available for {layerName}.");
        }

        private static void RemoveCurve2Leaves(IReadOnlyList<GameObject> candidates, BimWebGLCleanupSummary summary)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if ((i & 127) == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "BIM WebGL Cleanup",
                        $"Removing Curve_2 leaves ({i:N0}/{candidates.Count:N0})",
                        candidates.Count == 0 ? 1f : (float)i / candidates.Count * 0.35f);
                }

                GameObject candidate = candidates[i];
                if (candidate != null)
                {
                    Undo.DestroyObjectImmediate(candidate);
                    summary.RemovedCurve2Leaves++;
                }
            }
        }

        private static void ApplyLayersAndColliders(
            Scene scene,
            int selectableLayer,
            int bimInspectableLayer,
            int cameraCollisionLayer,
            BimWebGLCleanupSummary summary)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                if ((i & 127) == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "BIM WebGL Cleanup",
                        $"Assigning layers and simplifying colliders ({i:N0}/{transforms.Length:N0})",
                        0.35f + (transforms.Length == 0 ? 0.65f : (float)i / transforms.Length * 0.65f));
                }

                Transform transform = transforms[i];
                if (transform == null || transform.gameObject.scene != scene)
                {
                    continue;
                }

                GameObject gameObject = transform.gameObject;
                bool isUnit = gameObject.CompareTag("Unit");
                bool isBackgroundWall = gameObject.CompareTag("BackgroundWall");
                bool isBimInspectable = gameObject.GetComponent<Metadata>() != null
                    || (transform.parent != null && transform.parent.GetComponent<Metadata>() != null);

                if (isUnit)
                {
                    if (SetLayer(gameObject, selectableLayer))
                    {
                        summary.AssignedSelectableLayer++;
                    }
                }
                else if (isBackgroundWall)
                {
                    if (SetLayer(gameObject, cameraCollisionLayer))
                    {
                        summary.AssignedCameraCollisionLayer++;
                    }
                }
                else if (isBimInspectable)
                {
                    if (SetLayer(gameObject, bimInspectableLayer))
                    {
                        summary.AssignedBimInspectableLayer++;
                    }
                }

                MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
                BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
                if (isBackgroundWall
                    && meshCollider == null
                    && boxCollider != null
                    && BimWebGLOptimizationAnalyzer.HasNegativeLossyScale(transform))
                {
                    Mesh sharedMesh = BimWebGLOptimizationAnalyzer.GetSharedMesh(gameObject);
                    if (sharedMesh != null)
                    {
                        RestoreMeshCollider(gameObject, boxCollider, sharedMesh);
                        summary.RestoredNegativeScaleMeshColliders++;
                        meshCollider = gameObject.GetComponent<MeshCollider>();
                    }
                }

                if (meshCollider == null)
                {
                    continue;
                }

                if (!isUnit && !isBimInspectable && !isBackgroundWall)
                {
                    Undo.DestroyObjectImmediate(meshCollider);
                    summary.RemovedVisualMeshColliders++;
                }
                else if (isBackgroundWall
                    && meshCollider.sharedMesh != null
                    && !BimWebGLOptimizationAnalyzer.HasNegativeLossyScale(transform))
                {
                    ConvertToBoxCollider(gameObject, meshCollider);
                    summary.ConvertedCameraCollisionProxies++;
                }
            }
        }

        private static bool SetLayer(GameObject gameObject, int layer)
        {
            if (gameObject.layer == layer)
            {
                return false;
            }

            Undo.RecordObject(gameObject, "Assign BIM WebGL Layer");
            gameObject.layer = layer;
            return true;
        }

        private static void ConvertToBoxCollider(GameObject gameObject, MeshCollider meshCollider)
        {
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider>(gameObject);
                Bounds meshBounds = meshCollider.sharedMesh.bounds;
                boxCollider.center = meshBounds.center;
                boxCollider.size = meshBounds.size;
                boxCollider.isTrigger = meshCollider.isTrigger;
                boxCollider.sharedMaterial = meshCollider.sharedMaterial;
            }

            Undo.DestroyObjectImmediate(meshCollider);
        }

        private static void RestoreMeshCollider(GameObject gameObject, BoxCollider boxCollider, Mesh sharedMesh)
        {
            MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(gameObject);
            meshCollider.sharedMesh = sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = boxCollider.isTrigger;
            meshCollider.sharedMaterial = boxCollider.sharedMaterial;
            Undo.DestroyObjectImmediate(boxCollider);
        }

        private static void ConfigureRuntimeRaycastMasks(
            Scene scene,
            int selectableLayer,
            int bimInspectableLayer,
            int cameraCollisionLayer)
        {
            int cameraMask = 1 << cameraCollisionLayer;
            int selectableMask = (1 << selectableLayer) | (1 << bimInspectableLayer) | cameraMask;

            OrbitCamera[] orbitCameras = UnityEngine.Object.FindObjectsByType<OrbitCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (OrbitCamera orbitCamera in orbitCameras)
            {
                if (orbitCamera.gameObject.scene == scene)
                {
                    SetSerializedLayerMask(orbitCamera, "cameraCollisionMask", cameraMask);
                }
            }

            HoverManager[] hoverManagers = UnityEngine.Object.FindObjectsByType<HoverManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (HoverManager hoverManager in hoverManagers)
            {
                if (hoverManager.gameObject.scene == scene)
                {
                    SetSerializedLayerMask(hoverManager, "selectableMask", selectableMask);
                }
            }
        }

        private static void ValidateSummary(BimWebGLSceneReport report, BimWebGLCleanupSummary summary)
        {
            if (summary.RemovedCurve2Leaves != report.TransformOnlyCurve2Count
                || summary.AssignedSelectableLayer != report.SelectableLayerAssignmentCount
                || summary.AssignedBimInspectableLayer != report.BimInspectableLayerAssignmentCount
                || summary.AssignedCameraCollisionLayer != report.CameraCollisionLayerAssignmentCount
                || summary.RemovedVisualMeshColliders != report.RemovableVisualMeshColliderCount
                || summary.ConvertedCameraCollisionProxies != report.BackgroundWallBoxProxyCount
                || summary.RestoredNegativeScaleMeshColliders != report.BackgroundWallInvalidBoxProxyCount
                || summary.DisabledMeshReadWriteImporters != report.MeshReadWriteDisableCandidateCount)
            {
                throw new InvalidOperationException(
                    "Cleanup result did not match the approved dry-run counts. All scene changes will be reverted.");
            }
        }

        private static void DisableSafeMeshReadWrite(
            Scene scene,
            BimWebGLSceneReport approvedReport,
            ICollection<string> disabledPaths,
            BimWebGLCleanupSummary summary)
        {
            IReadOnlyList<string> currentPaths = BimWebGLOptimizationAnalyzer.FindSafeReadableMeshImporterPaths(scene);
            if (currentPaths.Count != approvedReport.MeshReadWriteDisableCandidatePaths.Count)
            {
                throw new InvalidOperationException("Safe mesh Read/Write candidates changed after the dry run.");
            }

            for (int i = 0; i < currentPaths.Count; i++)
            {
                string path = currentPaths[i];
                if (!string.Equals(path, approvedReport.MeshReadWriteDisableCandidatePaths[i], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Safe mesh Read/Write candidates changed after the dry run.");
                }

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || !importer.isReadable)
                {
                    throw new InvalidOperationException($"Model importer is no longer readable: {path}");
                }

                disabledPaths.Add(path);
                importer.isReadable = false;
                importer.SaveAndReimport();
                summary.DisabledMeshReadWriteImporters++;
            }
        }

        private static void RestoreMeshReadWrite(IEnumerable<string> disabledPaths)
        {
            foreach (string path in disabledPaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void SetSerializedLayerMask(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized layer mask {propertyName} was not found on {target.name}.");
            }

            if (property.intValue == value)
            {
                return;
            }

            Undo.RecordObject(target, "Configure BIM WebGL Raycast Mask");
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
