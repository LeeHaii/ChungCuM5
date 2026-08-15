# WebGL Performance Baseline

Captured on 2026-08-14 from `Assets/Scenes/AltScene.unity` before optimization. The working copy is `Assets/Scenes/AltScene_Optimized.unity`; the original scene was not modified.

## Environment

| Item | Value |
| --- | --- |
| Unity | 6000.3.17f1 |
| Render pipeline | Universal Render Pipeline 17.3.0 |
| Build target | WebGL Development |
| Build scene | `Assets/Scenes/AltScene_Optimized.unity` |
| Build output | `Builds/WebGLBaseline` |
| Build duration | 14 min 30 sec |
| Build result | Succeeded |

## Scene Baseline

| Metric | Baseline |
| --- | ---: |
| GameObjects | 27,543 |
| Active GameObjects | 27,102 |
| Inactive GameObjects | 441 |
| Root GameObjects | 13 |
| Renderers | 14,510 |
| MeshColliders | 14,342 |
| Triangles | 1,385,381 |
| Material slots | 17,803 |
| Empty leaf objects | 6,111 |
| Pixyz Metadata components | 14,362 |
| Loaded meshes | 641 |
| Loaded mesh memory | 11.798 MiB |
| Loaded textures | 1,336 |
| Loaded texture memory | 237.561 MiB |

The scene analyzer reported zero errors and four warnings. Counts were collected in Edit mode with `AltScene` active; the optimized scene is an AssetDatabase duplicate of that source.

## Rendering Snapshot

These values are an Edit-mode editor snapshot, not a WebGL gameplay benchmark. They are retained as a structural reference only.

| Metric | Snapshot |
| --- | ---: |
| Visible triangles | 782,689 |
| Visible vertices | 1,427,365 |
| Batches | 9,855 |
| SetPass calls | 62 |
| Draw calls | 10,393 |
| Static batched draws | 0 |
| Dynamic batched draws | 0 |
| Instanced batched draws | 0 |
| Total allocated memory | 636.549 MiB |
| Total reserved memory | 1,585.770 MiB |
| Mono heap | 1,300.418 MiB |
| Mono used | 1,209.809 MiB |

## Development Build Size

| File | Bytes | MiB |
| --- | ---: | ---: |
| `Build/WebGLBaseline.wasm` | 110,867,562 | 105.732 |
| `Build/WebGLBaseline.data` | 28,473,890 | 27.155 |
| `Build/WebGLBaseline.framework.js` | 848,369 | 0.809 |
| `Build/WebGLBaseline.loader.js` | 58,349 | 0.056 |
| `StreamingAssets/Database/ChungCuM5.db` | 57,344 | 0.055 |
| `StreamingAssets/Database/init_db.py` | 2,328 | 0.002 |
| `index.html` | 1,894 | 0.002 |
| `TemplateData/webmemd-icon.png` | 1,670 | 0.002 |
| **Total** | **140,311,406** | **133.811** |

## Runtime Measurements

The following require a running browser build and were not available from the editor automation snapshot:

- Time to first frame
- Idle FPS
- Continuous orbit FPS
- `GC.Alloc` per frame
- Physics time
- Main-thread rendering time
- WebGL peak memory

These measurements must be captured with the same camera overview, orbit, and selection scenarios after a browser-compatible runtime profiling setup is available. Editor-reported frame time and FPS are intentionally excluded because the scene was not in Play mode.

## Compilation

The baseline build completed with zero compilation errors. Unity reported two pre-existing obsolete-API warnings in `AppModeController.cs` and the Pixyz runtime plugin; no new warnings were introduced by this milestone.

## Final Comparison

Populate this table during Milestone 9 using the same scene and runtime scenarios.

| Metric | Baseline | Final | Change |
| --- | ---: | ---: | ---: |
| Startup time | Not captured | TBD | TBD |
| Build payload | 133.811 MiB | TBD | TBD |
| Peak memory | Not captured | TBD | TBD |
| GameObjects | 27,543 | TBD | TBD |
| Renderers | 14,510 | TBD | TBD |
| MeshColliders | 14,342 | TBD | TBD |
| Batches | 9,855 (Editor snapshot) | TBD | TBD |
| SetPass calls | 62 (Editor snapshot) | TBD | TBD |
| Idle frame time | Not captured | TBD | TBD |
| Orbit frame time | Not captured | TBD | TBD |
| Selection frame time | Not captured | TBD | TBD |
| Physics time | Not captured | TBD | TBD |
| GC allocation | Not captured | TBD | TBD |
