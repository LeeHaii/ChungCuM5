using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BimWebGLOptimization
{
    internal sealed class BimWebGLOptimizationWindow : EditorWindow
    {
        private const string OptimizedSceneSuffix = "_Optimized.unity";
        private static BimWebGLSceneReport lastReport;
        private Vector2 scrollPosition;

        [MenuItem("Tools/WebGL Optimization/Open Analyzer")]
        private static void Open()
        {
            GetWindow<BimWebGLOptimizationWindow>("BIM WebGL Optimization");
        }

        [MenuItem("Tools/WebGL Optimization/Analyze Active Scene (Dry Run)")]
        private static void AnalyzeFromMenu()
        {
            RunDryRun();
        }

        [MenuItem("Tools/WebGL Optimization/Remove Transform-Only Curve_2 Leaves")]
        private static void RemoveCurve2FromMenu()
        {
            RemoveVerifiedCurve2Leaves();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Active Scene", EditorStyles.boldLabel);
            Scene scene = SceneManager.GetActiveScene();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(scene.path) ? scene.name : scene.path);
            EditorGUILayout.Space();

            if (GUILayout.Button("Analyze Active Scene (Dry Run)", GUILayout.Height(28f)))
            {
                RunDryRun();
            }

            using (new EditorGUI.DisabledScope(lastReport == null || lastReport.ScenePath != scene.path))
            {
                if (GUILayout.Button("Remove Verified Curve_2 Leaves", GUILayout.Height(28f)))
                {
                    RemoveVerifiedCurve2Leaves();
                }
            }

            EditorGUILayout.Space();
            if (lastReport == null)
            {
                EditorGUILayout.HelpBox("Run the dry-run analyzer before enabling cleanup.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(lastReport.ToMarkdown(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static void RunDryRun()
        {
            try
            {
                lastReport = BimWebGLOptimizationAnalyzer.AnalyzeActiveScene(writeReport: true);
                Debug.Log(
                    $"BIM WebGL dry run complete for {lastReport.SceneName}: "
                    + $"{lastReport.TransformOnlyCurve2Count:N0} removable Curve_2 leaves, "
                    + $"{lastReport.MeshColliderCount:N0} MeshColliders, "
                    + $"{lastReport.MetadataComponentCount:N0} Metadata components.");
            }
            catch (Exception exception)
            {
                lastReport = null;
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("BIM WebGL Analysis Failed", exception.Message, "OK");
            }
        }

        private static void RemoveVerifiedCurve2Leaves()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Cleanup Blocked", "Load the optimized scene before cleanup.", "OK");
                return;
            }

            if (!scene.path.EndsWith(OptimizedSceneSuffix, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "Cleanup Blocked",
                    "Cleanup is restricted to a scene whose filename ends with _Optimized.unity.",
                    "OK");
                return;
            }

            if (scene.isDirty)
            {
                EditorUtility.DisplayDialog(
                    "Cleanup Blocked",
                    "The scene has unsaved changes. Save or revert them before running cleanup.",
                    "OK");
                return;
            }

            if (lastReport == null || lastReport.ScenePath != scene.path)
            {
                RunDryRun();
                EditorUtility.DisplayDialog(
                    "Dry Run Completed",
                    "Review the dry-run report, then run the cleanup command again.",
                    "OK");
                return;
            }

            List<GameObject> candidates = BimWebGLOptimizationAnalyzer.FindTransformOnlyCurve2Leaves(scene);
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("Cleanup", "No transform-only Curve_2 leaves were found.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Remove Transform-Only Curve_2 Leaves",
                $"Delete {candidates.Count:N0} verified transform-only Curve_2 objects from {scene.name}? "
                + "This operation is recorded as one Unity Undo group.",
                "Remove",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Transform-Only Curve_2 Leaves");

            try
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if ((i & 63) == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "BIM WebGL Cleanup",
                            $"Removing verified Curve_2 leaves ({i:N0}/{candidates.Count:N0})",
                            (float)i / candidates.Count);
                    }

                    GameObject candidate = candidates[i];
                    if (candidate != null)
                    {
                        Undo.DestroyObjectImmediate(candidate);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException($"Unity could not save {scene.path} after cleanup.");
                }

                Undo.CollapseUndoOperations(undoGroup);
                lastReport = null;
                Debug.Log($"Removed {candidates.Count:N0} transform-only Curve_2 leaves from {scene.path}.");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Cleanup Failed", "All cleanup changes were reverted. " + exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
