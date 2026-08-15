# WebGL Further Performance Optimization Plan

Date: 2026-08-15

Target scene: `Assets/Scenes/AltScene_Optimized.unity`

Unity: 6000.3.17f1

Render pipeline: URP 17.3.0

## Outcome

Turn the current optimized scene into a reproducible WebGL shipping build with lower startup cost, memory use, draw submission cost, and interaction-time allocation while preserving:

- per-element BIM hover and metadata selection;
- all 224 apartment-unit selection paths;
- orbit, wall collision, and first-person behavior;
- the current UI workflows and visual identity.

The plan is ordered by dependency and expected return. Each milestone must produce a comparable build and measurement report before the next structural change begins.

## Current Baseline and Audit Findings

### Existing progress worth preserving

- The original `AltScene.unity` remains intact and optimization work is isolated in `AltScene_Optimized.unity`.
- The first cleanup removed 6,109 empty `Curve_2` objects and reduced the scene from 27,513 to roughly 21,400 GameObjects.
- Collision layers now separate `Selectable`, `BimInspectable`, and `CameraCollision` queries.
- Camera motion, hover, header sync, and click selection no longer perform their former unconditional per-frame work.
- Camera collision uses non-allocating raycasts and hover results are reused by click selection.
- SRP Batcher is enabled and the WebGL default quality index selects the Mobile URP asset.
- The current Unity editor log has no C# compiler errors or runtime exception entries.

### Current scene structure

The current working copy contains:

| Metric | Current value | Interpretation |
| --- | ---: | --- |
| GameObjects | 21,177 | Still expensive to deserialize and retain in a browser player. |
| Active GameObjects | 21,107 | Almost the entire hierarchy loads at startup. |
| MeshRenderers | 14,506 | Primary source of draw submission and native object overhead. |
| MeshFilters | 14,506 | One-to-one renderer representation remains. |
| MeshColliders | 14,290 | Primary physics/native-memory scaling risk. |
| BoxColliders | 275 | Mostly lightweight unit and camera proxies. |
| Pixyz `Metadata` components | 14,362 | Primary scene serialization and managed-component issue. |
| Serialized Pixyz metadata | 29.86 MiB of scene YAML | 99% of serialized MonoBehaviour bytes in the scene. |
| Scene YAML size | 82.22 MiB | Down from 87.21 MiB, but still structurally large. |
| Source triangles | about 1.39 million | Not the first rendering bottleneck. |
| Material slots | 18,027 | More slots than renderers; contributes to draw splitting. |
| Repeated mesh/material groups | 417 groups / 14,568 instances | Strong candidate set for instancing or spatial clustering experiments. |
| Objects with regular static flags | 0 authored GameObjects | Static batching is not active. |
| Occlusion bake static renderers | 56 | Bake benefit is currently unmeasured and covers only a small static source set. |

The original editor snapshot reported 9,855 batches, 10,393 draw calls, and only 62 SetPass calls. This means shader/material state coherence is comparatively good; renderer count and draw submission are more important than broad material rewrites.

### Current WebGL build state

The only retained build is a Development build:

| Output | Current value |
| --- | ---: |
| Complete development build | 133.811 MiB |
| WebAssembly | 105.732 MiB |
| `.data` file | 27.155 MiB |
| User assets reported by Unity | 39.3 MiB |
| Stripped managed assemblies | 62 assemblies / 13.47 MiB before IL2CPP |
| IL2CPP global metadata | 8.98 MiB |

This is not a shipping-size baseline. Development builds are intentionally larger, and no Brotli transfer size, time to first frame, peak browser memory, WebGL CPU profile, or release frame-time sample has been captured.

The checked-in `EditorBuildSettings.asset` currently points to `Assets/Scenes/AltScene.unity`, not the optimized scene. Web build optimization settings also live partly in editor-local state, so the present build cannot be reproduced reliably from version control.

### Confirmed payload issues

- Textures account for 74.7% of Unity's reported user assets.
- `Cold Sunset Equirect.png` contributes 12.0 MiB to the build by itself. Its importer uses a 2048 maximum size, no mipmaps, and no Web-specific override.
- `TextMesh Pro/Examples & Extras/Resources` forces unused example fonts, sprite assets, textures, materials, and shaders into the player.
- Modern UI Pack's `Resources/Icon Library.asset` references 128 icon assets. `MUIP Manager.asset` also pulls in a broad font/theme dependency set, although the optimized scene uses only three `ModalWindowManager` components.
- Both Mobile and PC quality levels remain eligible on WebGL. Consequently, the build compiles PC renderer resources such as SSAO/deferred and additional post-processing resources even though WebGL defaults to Mobile quality.
- Addressables and ResourceManager remain in the stripped WebGL player, but there is no `AddressableAssetsData` configuration in the project. Addressables scripts are only referenced by `ApartmentScene.unity`; that scene is not in current Build Settings.
- The player includes the Pixyz runtime assembly because runtime code and 14,362 scene components directly depend on `Pixyz.ImportSDK.Metadata`.
- The build copies `StreamingAssets/Database/init_db.py` even though Python cannot run in the Web player, and the WebGL compilation path uses fake database data unless `USE_SQLITE` is defined.

### Confirmed runtime-code issues

- `FamilyDataViewController.HighlightUnit` reads and assigns `Renderer.materials`. This can instantiate per-renderer material copies and grow memory across selections.
- `ShowMetadata` asks Pixyz metadata to build a new `Dictionary<string,string>`, then destroys and recreates UI entry GameObjects for each selection.
- `UnitSearchTableController` and `FamilyDataViewController` destroy and instantiate complete row sets instead of reusing a bounded pool.
- `CameraWallFader` iterates all wall renderers and rewrites their property blocks every `LateUpdate`, even when the camera and resulting alpha values are unchanged.
- Release logging is not consistently gated. Some string interpolation and data enumeration can execute in player builds.

These code issues are worth fixing, but they rank below scene representation and build configuration because most are interaction-driven rather than continuous.

## Optimization Strategy

### Priority summary

| Priority | Workstream | Expected gain | Confidence |
| --- | --- | --- | --- |
| P0 | Reproducible release/profiling builds | Measurement quality, startup, download | Very high |
| P0 | Web-only quality and asset dependency cleanup | Download, GPU bandwidth, memory | High |
| P0 | Compact BIM metadata catalog | Startup, `.data`, managed/native memory, GC | Very high |
| P1 | Spatial render/collider clustering with element mapping | Frame time, physics, scalability, memory | High after prototype |
| P1 | Selection/UI pooling and dirty-driven updates | GC and interaction smoothness | High |
| P1 | Web memory and deployment tuning | Startup reliability and repeat load | High after profiling |
| P2 | Package/module stripping and shader cleanup | WebAssembly/download/build time | Medium; measure each removal |

## Milestone 0: Make Measurements Reproducible

Estimate: 0.5-1 day

### Work

1. Add a version-controlled WebGL build script or Build Profile that explicitly sets:
   - `AltScene_Optimized.unity` as the only startup scene;
   - Development + Autoconnect Profiler for profiling builds;
   - non-Development for release builds;
   - Brotli compression, no decompression fallback, and data caching;
   - the Web texture subtarget;
   - IL2CPP code-generation mode and Web code-optimization mode;
   - build output and a machine-readable measurement manifest.
2. Produce two release candidates:
   - Runtime Speed with LTO;
   - Disk Size with LTO.
3. Capture the same scenarios in each build:
   - cold page load to first interactive frame;
   - idle overview for 30 seconds;
   - continuous orbit/zoom for 30 seconds;
   - hover across BIM elements for 30 seconds;
   - ten metadata selections;
   - unit search and family-detail open/close;
   - first-person entry and movement if this flow remains in the shipping scope.
4. Record:
   - transferred Brotli bytes by file;
   - uncompressed `.wasm` and `.data` sizes;
   - time to first frame and first interaction;
   - Unity heap, browser process memory, and peak memory;
   - CPU frame time p50/p95, render thread/main thread time, physics time, and GC allocations;
   - editor Frame Debugger batches/draws for the same camera views.

### Exit gate

- A one-command profiling build and one-command release build are reproducible.
- A release baseline report replaces the current Development-build size as the optimization reference.
- All later milestones use the same browser, device, camera positions, and input recordings.

## Milestone 1: Remove Avoidable Web Payload and GPU Features

Estimate: 1-2 days

This milestone should happen before structural scene work because it is low risk and establishes the real size floor.

### Work

1. Create a Web-only quality tier and ensure the PC tier is excluded from Web builds.
2. Keep the Mobile renderer forward-only and test two Web quality variants:
   - Low: HDR and post-processing off, 0.75-0.8 render scale;
   - High: current HDR, vignette, and tonemapping behavior.
3. Disable Web shader features that the shipping scene does not use, including realtime shadow variants if the visual comparison confirms the current no-shadow behavior is intentional.
4. Build explicit texture variants:
   - DXT for desktop browsers;
   - ASTC for supported mobile browsers;
   - a clear compatibility fallback if one universal build is required.
5. A/B the sky at 2048, 1024, and 512. Select the smallest size that passes the standard camera-view screenshot comparison.
6. Remove or relocate `TextMesh Pro/Examples & Extras/Resources` after verifying no shipping asset references it.
7. Replace the broad Modern UI Resources dependencies with one of:
   - a trimmed icon library and Web-specific UI manager asset containing only used icons/fonts; or
   - simple first-party modal panel behavior, allowing the runtime Modern UI dependency to be removed.
8. Remove `init_db.py` from StreamingAssets. Decide whether Web uses a JSON/binary read-only dataset, a network API, or the existing fake-data fallback; do not ship unused database bootstrap files.
9. Decide the Addressables direction:
   - remove Addressables and its unused scripts from the Web player; or
   - configure it intentionally for later floor/cell streaming in Milestone 5.
10. Test High managed stripping and IL2CPP Optimize Size. Add narrow `link.xml` entries only for reflection-driven code that fails validation.

### Exit gate

- PC SSAO/deferred resources are absent from the Web build report.
- The shipping scene has no dependency on TMP Examples & Extras.
- The sky and UI dependency reductions have screenshot and functional approval.
- The stripped assembly list and release transfer sizes are recorded before/after.
- No regression in text rendering, modal behavior, touch input, or error handling.

## Milestone 2: Replace Component-Per-Element Pixyz Metadata

Estimate: 3-5 days

This is the highest-confidence structural optimization and a prerequisite for major collider reduction.

### Design

Create a compact runtime `BimMetadataCatalog` generated in the editor from Pixyz data:

- one deduplicated string table for property keys and repeated values;
- one flat property array containing key/value string indices;
- one element record array containing property offset/count and stable element ID;
- one scene binding table mapping target transforms or colliders to element indices during the transition;
- one runtime lookup API, for example `TryGetElement(Transform, out BimElementRecord)`;
- editor-only conversion/validation code that is allowed to reference Pixyz;
- runtime code with no Pixyz dependency.

Do not replace 14,362 large metadata components with 14,362 new general-purpose metadata components. During the transition, keep bindings in one catalog component or one generated asset and build a pre-sized lookup once at scene initialization.

### Work

1. Export the current Pixyz names/values into the compact catalog.
2. Add edit-mode validation that compares every converted element/property against the source metadata.
3. Change `HoverManager` and `ShowMetadata` to query an `IBimMetadataStore` instead of checking Pixyz component types.
4. Return lightweight property spans/enumerators; do not construct a dictionary per click.
5. Remove Pixyz metadata components from the optimized scene only after full conversion validation passes.
6. Move Pixyz conversion tooling to an Editor-only assembly and exclude the Pixyz runtime assembly from WebGL.
7. Pool metadata UI rows and populate them directly from the catalog.

### Exit gate

- 100% element/property conversion match on the generated validation report.
- BIM hover and selection resolve the same object and displayed metadata in representative wall, door, window, floor, and unit samples.
- Zero Pixyz `Metadata` components remain in `AltScene_Optimized`.
- Pixyz runtime code is absent from the stripped WebGL player.
- Record changes to scene YAML size, `.data`, startup time, heap, and selection allocation.

## Milestone 3: Reduce Renderers and MeshColliders by Spatial Clustering

Estimate: 5-10 days, including prototype and validation

The current triangle count is moderate; the 14,506 renderer and 14,290 MeshCollider objects are the scaling problem. Prototype on one representative floor before converting the whole building.

### Prototype design

1. Partition BIM geometry by floor and bounded spatial cell.
2. Combine render geometry within each cell by compatible material/shader.
3. Build one or a small number of non-convex collision meshes per cell.
4. Persist triangle-range-to-element-ID mappings for combined collision meshes.
5. Resolve `RaycastHit.triangleIndex` through that mapping to the compact metadata catalog.
6. Preserve the 224 apartment units as separate selectable objects unless the same mapping proves safe for them.
7. Render selection feedback without restoring a renderer per element. Candidate approaches:
   - a temporary overlay mesh for the selected element;
   - a cached source-mesh highlight renderer enabled only for the current selection;
   - an element-ID shader path only if WebGL compatibility and memory are better than the overlay approach.

### Rendering experiment

Compare three approaches on the same floor:

- current SRP Batcher path;
- explicit instancing for the largest repeated mesh/material groups;
- spatial mesh clustering.

Enabling material instancing alone is not a sufficient experiment because SRP Batcher takes priority for compatible URP renderers. Measure actual WebGL frame time and draw count. Do not enable global static batching as the default solution: it can duplicate vertex data and increase Web memory.

### Occlusion follow-up

After chunking, mark only stable building shells and suitable chunks as occluders/occludees, rebake, and compare visible renderer count and frame time in the standard views. Keep the bake only if it provides a measurable p95 improvement without excessive bake data or popping.

### Provisional structural targets

| Metric | Current | Prototype/full target |
| --- | ---: | ---: |
| GameObjects | 21,177 | under 5,000 |
| MeshRenderers | 14,506 | under 2,000 |
| MeshColliders | 14,290 | under 500 |
| Batches in standard overview | 9,855 | under 2,000 |
| Source triangles | about 1.39 M | no more than 10% growth |

Targets are accepted only if per-element selection remains correct and peak memory improves. A lower draw count that raises browser memory or breaks interaction is not a win.

### Exit gate

- The one-floor prototype passes selection/metadata/highlight tests for every mapped element.
- Full conversion proceeds only if the prototype improves p95 orbit frame time or peak memory by at least 15%.
- The final mapping tool produces deterministic output and a coverage report with no unmapped selectable triangles.

## Milestone 4: Remove Interaction Spikes and Idle Work

Estimate: 1-2 days

### Work

1. Replace unit highlight material cloning with a shared `MaterialPropertyBlock` or a single reusable overlay renderer.
2. Pool unit-table, family-detail, and BIM metadata rows. Update existing rows and disable surplus rows instead of destroying them.
3. Cache row child references in a row-view component instead of repeated `Transform.Find` and `GetComponent` calls during population.
4. Make `CameraWallFader` dirty-driven:
   - update only when the camera position or hovered wall changes;
   - cache material property IDs and base colors;
   - skip `SetPropertyBlock` when alpha is unchanged;
   - use squared distance where exact distance is unnecessary.
5. Gate informational logging with `UNITY_EDITOR` or `DEVELOPMENT_BUILD`; avoid evaluating interpolation/helper calls in release builds.
6. Pre-size lists/lookups created during startup from known catalog counts.

### Exit gate

- Idle and continuous orbit report 0 B/frame managed allocation in the standard samples.
- Ten repeated selections do not increase material instance count.
- Metadata and unit UI selection allocation is at least 75% lower than the release baseline.
- Wall fading remains visually identical and does no work while camera/hover state is unchanged.

## Milestone 5: Tune Web Memory and Content Delivery

Estimate: 1-2 days after Milestones 1-4

Unity Web keeps the uncompressed startup `.data` content in browser memory and grows a contiguous WebAssembly heap. Tune these settings only after the content footprint is reduced.

### Work

1. Measure typical and peak Unity heap on reference desktop and mobile devices.
2. Replace the current 32 MiB initial heap with a measured value that avoids repeated startup growth while retaining a safe browser-memory margin.
3. Reduce the current 2048 MiB maximum if profiling proves it unnecessary; test heap-growth failures explicitly on low-memory mobile devices.
4. If startup `.data` or peak memory remains above budget, stream BIM floors/cells as Addressables/AssetBundles and unload non-visible cells. Do not adopt streaming until Milestone 3 defines deterministic spatial chunks.
5. Validate deployment headers:
   - `Content-Encoding: br` for Brotli files;
   - `Content-Type: application/wasm` for WebAssembly;
   - immutable caching for hashed build files;
   - no JavaScript decompression fallback in the shipping configuration.
6. Confirm IndexedDB data caching and second-load behavior.

### Exit gate

- No out-of-memory or failed heap-growth event in the reference device matrix.
- Cold and warm startup measurements are recorded.
- Server headers are covered by an automated deployment smoke test.

## Validation Matrix and Performance Budgets

### Reference matrix

At minimum:

- Chrome and Edge on the reference desktop;
- Chrome on one 4-6 GB Android device;
- one integrated-GPU laptop or comparable low-tier desktop;
- desktop DXT and mobile ASTC build variants where supported.

### Correctness suite

- Load optimized scene without console errors.
- Orbit, drag, zoom, reset, and camera-wall collision.
- Hover and select representative BIM wall, window, door, floor, and miscellaneous element.
- Verify metadata keys/values before and after catalog conversion.
- Select every apartment unit programmatically and verify collider/ID coverage.
- Open/close BIM, unit search, family detail, and collapse panels.
- Touch input and UI pointer blocking.
- First-person transition, doors, movement, and return flow if included in Web scope.

### Provisional budgets

The first release baseline in Milestone 0 may refine absolute startup/memory values. These structural and allocation budgets should remain:

| Metric | Budget |
| --- | --- |
| Idle GC allocation | 0 B/frame median |
| Orbit GC allocation | 0 B/frame median |
| Desktop p95 frame time | at or below 16.7 ms on the reference desktop |
| Mobile/low-tier p95 frame time | at or below 33.3 ms |
| Per-milestone targeted metric | at least 10% improvement, unless the milestone is a prerequisite |
| Non-target metric regression | no more than 5% without explicit approval |
| Renderer/collider mapping coverage | 100% of selectable BIM elements |

Use the Milestone 0 release results to set final transfer-size, startup-time, and peak-memory budgets. Do not compare release results against the 133.8 MiB Development build as though they were equivalent artifacts.

## Work Not Worth Doing Yet

- Do not start with broad triangle decimation or LOD generation. The scene has about 1.39 million source triangles but nearly 10,000 batches; object and draw count are the stronger evidence.
- Do not globally enable static batching. Browser memory is already a concern, and static batching can duplicate geometry.
- Do not rewrite the project to ECS/DOTS. The current bottleneck can be addressed with generated catalogs and spatial chunks inside the existing architecture.
- Do not enable multithreaded WebAssembly before the single-threaded content path is lean. Threads add deployment/header constraints and do not solve component, draw, collider, or payload counts.
- Do not micro-optimize startup-only `Find`/`GetComponent` calls before metadata, asset, renderer, and collider work.
- Do not compress audio: the current build report contains no audio payload.
- Do not remove packages solely because they appear in `manifest.json`; use the stripped assembly/build report and a before/after build to prove player impact.

## Recommended Execution Order

1. Milestone 0: reproducible release baseline.
2. Milestone 1: Web-only quality, texture, Resources, and build dependency cleanup.
3. Milestone 2: compact metadata catalog and Pixyz player removal.
4. Milestone 3 prototype: one-floor spatial render/collider mapping.
5. Milestone 4 can run after the metadata API stabilizes; its UI pooling work should use the new catalog directly.
6. Complete Milestone 3 only if the prototype passes its 15% gain gate.
7. Milestone 5: heap and streaming decisions using the reduced final content.
8. Produce the final baseline comparison using the table in `WebGLPerformanceBaseline.md`.

## Project and Unity References

Project evidence:

- `Docs/WebGLPerformanceBaseline.md`
- `Docs/WebGLOptimizationDryRun.md`
- `Docs/WebGLMilestone4Validation.md`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Settings/Mobile_RPAsset.asset`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Scripts/Bim/ShowMetadata.cs`
- `Assets/Scripts/UI/FamilyDataViewController.cs`
- `Assets/Scripts/Camera/CameraWallFader.cs`

Unity 6.3 guidance:

- [Memory in Unity Web](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-memory.html)
- [Web build settings reference](https://docs.unity3d.com/6000.3/Documentation/Manual/web-build-settings.html)
- [Web texture compression](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-texture-compression.html)
- [Deploy a Web application](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html)
- [Choose a draw-call optimization method](https://docs.unity3d.com/6000.3/Documentation/Manual/optimizing-draw-calls-choose-method.html)
