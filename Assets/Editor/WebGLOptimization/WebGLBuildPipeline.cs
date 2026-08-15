using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BimRuntime;
using Pixyz.ImportSDK;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.WebGL;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace BimWebGLOptimization
{
    internal enum WebGLBuildKind
    {
        Profiling,
        RuntimeSpeedLto,
        DiskSizeLto
    }

    internal enum WebGLQualityVariant
    {
        High,
        Low
    }

    [Serializable]
    internal sealed class WebGLBuildFileManifest
    {
        public string path;
        public long bytes;
    }

    [Serializable]
    internal sealed class WebGLBuildManifest
    {
        public string generatedUtc;
        public string unityVersion;
        public string scene;
        public string outputPath;
        public string buildKind;
        public string qualityVariant;
        public string textureSubtarget;
        public string wasmCodeOptimization;
        public string il2CppCodeGeneration;
        public string result;
        public bool development;
        public bool autoConnectProfiler;
        public bool brotli;
        public bool decompressionFallback;
        public bool dataCaching;
        public bool hashedFileNames;
        public int skyMaxSize;
        public double buildSeconds;
        public long reportedTotalBytes;
        public int errors;
        public int warnings;
        public WebGLBuildFileManifest[] files;
    }

    internal static class WebGLBuildPipeline
    {
        internal const string ShippingScene = "Assets/Scenes/AltScene_Optimized.unity";
        private const string MobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
        private const string UrpGlobalSettingsPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";
        private const string SkyTexturePath = "Assets/AllSkyFree/Cold Sunset/Cold Sunset Equirect.png";
        private const string DefaultOutputRoot = "Builds/WebGL";
        private const string ManifestOutputRoot = "Docs/WebGLBuildManifests";

        [MenuItem("Tools/WebGL Optimization/Build/Profiling DXT High")]
        public static void BuildProfilingDxtHigh()
        {
            Build(WebGLBuildKind.Profiling, WebGLTextureSubtarget.DXT, WebGLQualityVariant.High, 2048, null);
        }

        [MenuItem("Tools/WebGL Optimization/Build/Release Runtime Speed LTO DXT High")]
        public static void BuildReleaseRuntimeDxtHigh()
        {
            Build(WebGLBuildKind.RuntimeSpeedLto, WebGLTextureSubtarget.DXT, WebGLQualityVariant.High, 2048, null);
        }

        [MenuItem("Tools/WebGL Optimization/Build/Release Disk Size LTO DXT High")]
        public static void BuildReleaseSizeDxtHigh()
        {
            Build(WebGLBuildKind.DiskSizeLto, WebGLTextureSubtarget.DXT, WebGLQualityVariant.High, 2048, null);
        }

        [MenuItem("Tools/WebGL Optimization/Build/Release Disk Size LTO ASTC High")]
        public static void BuildReleaseSizeAstcHigh()
        {
            Build(WebGLBuildKind.DiskSizeLto, WebGLTextureSubtarget.ASTC, WebGLQualityVariant.High, 2048, null);
        }

        [MenuItem("Tools/WebGL Optimization/Build/Release Disk Size LTO DXT Low (Sky 1024)")]
        public static void BuildReleaseSizeDxtLow()
        {
            Build(WebGLBuildKind.DiskSizeLto, WebGLTextureSubtarget.DXT, WebGLQualityVariant.Low, 1024, null);
        }

        [MenuItem("Tools/WebGL Optimization/Build/Release Disk Size LTO ASTC Low (Sky 1024)")]
        public static void BuildReleaseSizeAstcLow()
        {
            Build(WebGLBuildKind.DiskSizeLto, WebGLTextureSubtarget.ASTC, WebGLQualityVariant.Low, 1024, null);
        }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            WebGLBuildKind kind = ParseEnum(GetArgument(args, "--webgl-mode"), WebGLBuildKind.DiskSizeLto);
            WebGLTextureSubtarget texture = ParseEnum(GetArgument(args, "--webgl-texture"), WebGLTextureSubtarget.DXT);
            WebGLQualityVariant quality = ParseEnum(GetArgument(args, "--webgl-quality"), WebGLQualityVariant.High);
            int skyMaxSize = ParseSkyMaxSize(GetArgument(args, "--sky-max-size"), quality == WebGLQualityVariant.Low ? 1024 : 2048);
            string output = GetArgument(args, "--webgl-output");
            Build(kind, texture, quality, skyMaxSize, output);
        }

        private static void Build(
            WebGLBuildKind kind,
            WebGLTextureSubtarget texture,
            WebGLQualityVariant quality,
            int skyMaxSize,
            string outputOverride)
        {
            ValidateSourceConfiguration();
            string outputPath = string.IsNullOrWhiteSpace(outputOverride)
                ? GetDefaultOutputPath(kind, texture, quality, skyMaxSize)
                : outputOverride.Replace('\\', '/').TrimEnd('/');
            Directory.CreateDirectory(outputPath);

            WebGLBuildSettingsSnapshot snapshot = new WebGLBuildSettingsSnapshot();
            BuildReport report = null;
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                ConfigureBuild(kind, texture, quality, skyMaxSize);
                WebGLBuildSceneProcessor.QualityVariant = quality;

                BuildOptions options = BuildOptions.None;
                if (kind == WebGLBuildKind.Profiling)
                {
                    options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
                }

                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ShippingScene },
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = options
                });
            }
            finally
            {
                stopwatch.Stop();
                WebGLBuildSceneProcessor.QualityVariant = WebGLQualityVariant.High;
                snapshot.Restore();
            }

            if (report == null)
            {
                throw new BuildFailedException("Unity did not return a WebGL build report.");
            }

            WebGLBuildManifest manifest = CreateManifest(
                report,
                kind,
                texture,
                quality,
                skyMaxSize,
                outputPath,
                stopwatch.Elapsed.TotalSeconds);
            WriteManifest(manifest, outputPath);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL {kind} build failed with {report.summary.totalErrors:N0} errors. See the Editor log.");
            }

            Debug.Log(
                $"WebGL {kind} {texture}/{quality} build succeeded: {report.summary.totalSize:N0} bytes in "
                + $"{report.summary.totalTime}. Manifest written beside the build and under {ManifestOutputRoot}.");
        }

        private static void ConfigureBuild(
            WebGLBuildKind kind,
            WebGLTextureSubtarget texture,
            WebGLQualityVariant quality,
            int skyMaxSize)
        {
            bool profiling = kind == WebGLBuildKind.Profiling;
            EditorUserBuildSettings.webGLBuildSubtarget = texture;
            UserBuildSettings.codeOptimization = kind switch
            {
                WebGLBuildKind.RuntimeSpeedLto => WasmCodeOptimization.RuntimeSpeedLTO,
                WebGLBuildKind.DiskSizeLto => WasmCodeOptimization.DiskSizeLTO,
                _ => WasmCodeOptimization.RuntimeSpeed
            };

            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.WebGL,
                kind == WebGLBuildKind.DiskSizeLto ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.debugSymbolMode = profiling ? WebGLDebugSymbolMode.External : WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = profiling
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.usePlayerLog = profiling;

            ConfigureMobilePipeline(quality);
            ConfigureSkyTexture(skyMaxSize);
        }

        private static void ConfigureMobilePipeline(WebGLQualityVariant quality)
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelinePath);
            if (pipeline == null) throw new BuildFailedException($"Mobile URP asset not found: {MobilePipelinePath}");

            SerializedObject serialized = new SerializedObject(pipeline);
            SerializedProperty hdr = serialized.FindProperty("m_SupportsHDR");
            SerializedProperty renderScale = serialized.FindProperty("m_RenderScale");
            if (hdr == null || renderScale == null)
                throw new BuildFailedException("Mobile URP HDR/render-scale properties could not be found.");

            hdr.boolValue = quality == WebGLQualityVariant.High;
            renderScale.floatValue = quality == WebGLQualityVariant.Low ? 0.8f : 0.8f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSkyTexture(int skyMaxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkyTexturePath) as TextureImporter;
            if (importer == null) throw new BuildFailedException($"Sky texture importer not found: {SkyTexturePath}");

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = skyMaxSize;
            settings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ValidateSourceConfiguration()
        {
            if (!File.Exists(ShippingScene)) throw new BuildFailedException($"Shipping scene not found: {ShippingScene}");

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length != 1 || !buildScenes[0].enabled || buildScenes[0].path != ShippingScene)
            {
                throw new BuildFailedException(
                    $"Editor Build Settings must contain only the enabled shipping scene {ShippingScene}.");
            }

            int pcQualityIndex = Array.IndexOf(QualitySettings.names, "PC");
            if (pcQualityIndex < 0) throw new BuildFailedException("The PC quality tier was not found.");
            if (QualitySettings.GetQualityLevel() < 0) throw new BuildFailedException("No active quality tier is available.");

            if (Directory.Exists("Assets/TextMesh Pro/Examples & Extras/Resources"))
                throw new BuildFailedException("TMP Examples & Extras still contains a Resources folder.");
            if (File.Exists("Assets/StreamingAssets/Database/init_db.py"))
                throw new BuildFailedException("Unused init_db.py is still present in StreamingAssets.");
        }

        private static WebGLBuildManifest CreateManifest(
            BuildReport report,
            WebGLBuildKind kind,
            WebGLTextureSubtarget texture,
            WebGLQualityVariant quality,
            int skyMaxSize,
            string outputPath,
            double buildSeconds)
        {
            List<WebGLBuildFileManifest> files = new List<WebGLBuildFileManifest>();
            if (Directory.Exists(outputPath))
            {
                string root = Path.GetFullPath(outputPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string[] paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
                Array.Sort(paths, StringComparer.Ordinal);
                for (int i = 0; i < paths.Length; i++)
                {
                    FileInfo info = new FileInfo(paths[i]);
                    string relative = info.FullName.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    files.Add(new WebGLBuildFileManifest { path = relative.Replace('\\', '/'), bytes = info.Length });
                }
            }

            return new WebGLBuildManifest
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                scene = ShippingScene,
                outputPath = outputPath,
                buildKind = kind.ToString(),
                qualityVariant = quality.ToString(),
                textureSubtarget = texture.ToString(),
                wasmCodeOptimization = kind == WebGLBuildKind.RuntimeSpeedLto
                    ? WasmCodeOptimization.RuntimeSpeedLTO.ToString()
                    : kind == WebGLBuildKind.DiskSizeLto
                        ? WasmCodeOptimization.DiskSizeLTO.ToString()
                        : WasmCodeOptimization.RuntimeSpeed.ToString(),
                il2CppCodeGeneration = kind == WebGLBuildKind.DiskSizeLto ? "OptimizeSize" : "OptimizeSpeed",
                result = report.summary.result.ToString(),
                development = kind == WebGLBuildKind.Profiling,
                autoConnectProfiler = kind == WebGLBuildKind.Profiling,
                brotli = true,
                decompressionFallback = false,
                dataCaching = true,
                hashedFileNames = true,
                skyMaxSize = skyMaxSize,
                buildSeconds = buildSeconds,
                reportedTotalBytes = (long)report.summary.totalSize,
                errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings,
                files = files.ToArray()
            };
        }

        private static void WriteManifest(WebGLBuildManifest manifest, string outputPath)
        {
            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(Path.Combine(outputPath, "webgl-build-manifest.json"), json, new UTF8Encoding(false));
            Directory.CreateDirectory(ManifestOutputRoot);
            string name = $"{manifest.buildKind}-{manifest.textureSubtarget}-{manifest.qualityVariant}-sky{manifest.skyMaxSize}.json";
            File.WriteAllText(Path.Combine(ManifestOutputRoot, name), json, new UTF8Encoding(false));
        }

        private static string GetDefaultOutputPath(
            WebGLBuildKind kind,
            WebGLTextureSubtarget texture,
            WebGLQualityVariant quality,
            int skyMaxSize)
        {
            return $"{DefaultOutputRoot}/{kind}-{texture}-{quality}-Sky{skyMaxSize}";
        }

        private static string GetArgument(IReadOnlyList<string> args, string name)
        {
            for (int i = 0; i < args.Count - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }

            return null;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return !string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
        }

        private static int ParseSkyMaxSize(string value, int fallback)
        {
            if (!int.TryParse(value, out int parsed)) return fallback;
            if (parsed == 512 || parsed == 1024 || parsed == 2048) return parsed;
            throw new ArgumentOutOfRangeException(nameof(value), "Sky max size must be 512, 1024, or 2048.");
        }

        private sealed class WebGLBuildSettingsSnapshot
        {
            private readonly WebGLTextureSubtarget textureSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;
            private readonly WasmCodeOptimization codeOptimization = UserBuildSettings.codeOptimization;
            private readonly Il2CppCodeGeneration codeGeneration = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL);
            private readonly ManagedStrippingLevel stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL);
            private readonly WebGLCompressionFormat compression = PlayerSettings.WebGL.compressionFormat;
            private readonly bool decompressionFallback = PlayerSettings.WebGL.decompressionFallback;
            private readonly bool dataCaching = PlayerSettings.WebGL.dataCaching;
            private readonly bool hashedNames = PlayerSettings.WebGL.nameFilesAsHashes;
            private readonly WebGLLinkerTarget linkerTarget = PlayerSettings.WebGL.linkerTarget;
            private readonly WebGLDebugSymbolMode debugSymbols = PlayerSettings.WebGL.debugSymbolMode;
            private readonly WebGLExceptionSupport exceptionSupport = PlayerSettings.WebGL.exceptionSupport;
            private readonly bool playerLog = PlayerSettings.usePlayerLog;
            private readonly bool mobileHdr;
            private readonly float mobileRenderScale;
            private readonly TextureImporterPlatformSettings skySettings;
            private readonly byte[] skyMetaFile;
            private readonly byte[] urpGlobalSettingsFile;

            public WebGLBuildSettingsSnapshot()
            {
                UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelinePath);
                SerializedObject serialized = pipeline != null ? new SerializedObject(pipeline) : null;
                mobileHdr = serialized?.FindProperty("m_SupportsHDR")?.boolValue ?? true;
                mobileRenderScale = serialized?.FindProperty("m_RenderScale")?.floatValue ?? 1f;
                TextureImporter importer = AssetImporter.GetAtPath(SkyTexturePath) as TextureImporter;
                skySettings = importer?.GetPlatformTextureSettings("WebGL");
                string skyMetaPath = SkyTexturePath + ".meta";
                skyMetaFile = File.Exists(skyMetaPath) ? File.ReadAllBytes(skyMetaPath) : null;
                urpGlobalSettingsFile = File.Exists(UrpGlobalSettingsPath)
                    ? File.ReadAllBytes(UrpGlobalSettingsPath)
                    : null;
            }

            public void Restore()
            {
                EditorUserBuildSettings.webGLBuildSubtarget = textureSubtarget;
                UserBuildSettings.codeOptimization = codeOptimization;
                PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, codeGeneration);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, stripping);
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = decompressionFallback;
                PlayerSettings.WebGL.dataCaching = dataCaching;
                PlayerSettings.WebGL.nameFilesAsHashes = hashedNames;
                PlayerSettings.WebGL.linkerTarget = linkerTarget;
                PlayerSettings.WebGL.debugSymbolMode = debugSymbols;
                PlayerSettings.WebGL.exceptionSupport = exceptionSupport;
                PlayerSettings.usePlayerLog = playerLog;

                UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelinePath);
                if (pipeline != null)
                {
                    SerializedObject serialized = new SerializedObject(pipeline);
                    SerializedProperty hdr = serialized.FindProperty("m_SupportsHDR");
                    SerializedProperty renderScale = serialized.FindProperty("m_RenderScale");
                    if (hdr != null) hdr.boolValue = mobileHdr;
                    if (renderScale != null) renderScale.floatValue = mobileRenderScale;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                TextureImporter importer = AssetImporter.GetAtPath(SkyTexturePath) as TextureImporter;
                if (skyMetaFile != null)
                {
                    File.WriteAllBytes(SkyTexturePath + ".meta", skyMetaFile);
                    AssetDatabase.ImportAsset(SkyTexturePath, ImportAssetOptions.ForceUpdate);
                }
                else if (importer != null && skySettings != null)
                {
                    importer.SetPlatformTextureSettings(skySettings);
                    importer.SaveAndReimport();
                }

                if (urpGlobalSettingsFile != null)
                {
                    File.WriteAllBytes(UrpGlobalSettingsPath, urpGlobalSettingsFile);
                    AssetDatabase.ImportAsset(UrpGlobalSettingsPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }

    internal sealed class WebGLBuildSceneProcessor : IProcessSceneWithReport
    {
        internal static WebGLQualityVariant QualityVariant { get; set; } = WebGLQualityVariant.High;
        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL || scene.path != WebGLBuildPipeline.ShippingScene)
                return;

            Metadata[] metadata = GetSceneComponents<Metadata>(scene);
            if (metadata.Length != 0)
            {
                throw new BuildFailedException(
                    $"The WebGL shipping scene still contains {metadata.Length:N0} Pixyz Metadata components. "
                    + "Run the validated BIM catalog conversion first.");
            }

            BimMetadataStore[] stores = GetSceneComponents<BimMetadataStore>(scene);
            if (stores.Length != 1 || stores[0].Catalog == null || stores[0].ElementCount == 0)
                throw new BuildFailedException("The WebGL shipping scene must contain one populated BimMetadataStore.");

            Collider[] colliders = GetSceneComponents<Collider>(scene);
            int selectableUnits = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].CompareTag("Unit")) selectableUnits++;
            }

            if (selectableUnits != 224)
                throw new BuildFailedException($"Expected 224 selectable unit collider paths but found {selectableUnits:N0}.");

            if (QualityVariant == WebGLQualityVariant.Low)
            {
                Camera[] cameras = GetSceneComponents<Camera>(scene);
                for (int i = 0; i < cameras.Length; i++)
                {
                    cameras[i].allowHDR = false;
                    UniversalAdditionalCameraData additionalData = cameras[i].GetComponent<UniversalAdditionalCameraData>();
                    if (additionalData != null) additionalData.renderPostProcessing = false;
                }
            }
        }

        private static T[] GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }
    }
}
