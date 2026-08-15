# WebGL Build and Validation

The shipping scene is `Assets/Scenes/AltScene_Optimized.unity`. Build commands reject any scene that still contains Pixyz `Metadata`, does not contain exactly one populated compact metadata store, or loses any of the 224 apartment-unit collider paths.

## Editor menu

- `Tools > WebGL Optimization > Metadata > Dry Run and Validate Catalog`
- `Tools > WebGL Optimization > Metadata > Convert Validated Pixyz Metadata`
- `Tools > WebGL Optimization > Build > Profiling DXT High`
- `Tools > WebGL Optimization > Build > Release Runtime Speed LTO DXT High`
- `Tools > WebGL Optimization > Build > Release Disk Size LTO DXT High`
- `Tools > WebGL Optimization > Build > Release Disk Size LTO ASTC High`
- `Tools > WebGL Optimization > Build > Release Disk Size LTO DXT Low (Sky 1024)`
- `Tools > WebGL Optimization > Spatial Prototype > Build From Selected Floor`

The spatial command is deliberately a reversible, selected-floor prototype. Do not retain it unless WebGL p95 orbit frame time or peak memory improves by at least 15% and every mapped element on the floor passes selection/metadata/highlight verification.

## Command line

```powershell
& '<Unity.exe>' -batchmode -quit -projectPath '<project>' `
  -executeMethod BimWebGLOptimization.WebGLBuildPipeline.BuildFromCommandLine `
  --webgl-mode DiskSizeLto --webgl-texture DXT --webgl-quality High `
  --sky-max-size 2048 --webgl-output Builds/WebGL/Release-DXT
```

Accepted build modes are `Profiling`, `RuntimeSpeedLto`, and `DiskSizeLto`. Texture targets are `DXT` and `ASTC`; quality variants are `High` and `Low`; sky candidates are `512`, `1024`, and `2048`.

Each build writes `webgl-build-manifest.json` beside the build and a comparable copy under `Docs/WebGLBuildManifests`. The manifest records settings, result, duration, Unity-reported size, and every output file size.

## Measurement sequence

Use the same browser/device, camera positions, and inputs for each candidate:

1. Cold load to first interactive frame.
2. Idle overview for 30 seconds.
3. Orbit/zoom for 30 seconds.
4. BIM hover for 30 seconds.
5. Ten metadata selections.
6. Unit search and family detail open/close.
7. First-person entry and movement.

Record transferred Brotli bytes, uncompressed `.wasm`/`.data`, startup, Unity/browser peak memory, CPU p50/p95, render/main/physics time, GC allocation, and matching Frame Debugger draws.

## Deployment smoke test

After deployment, pass the hashed `.wasm.br` and `.data.br` paths from the build manifest:

```powershell
./Tools/Verify-WebGLHeaders.ps1 `
  -BuildBaseUrl 'https://example.invalid/Build' `
  -WasmFile 'app.wasm.br' -DataFile 'app.data.br'
```

The test requires Brotli content encoding, `application/wasm`, and immutable caching.
