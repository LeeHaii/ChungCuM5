# WebGL Further Optimization Implementation

Implemented on 2026-08-15 against Unity 6000.3.17f1 and `Assets/Scenes/AltScene_Optimized.unity`.

## Outcome

- The shipping build contains only `AltScene_Optimized` and uses a dedicated WebGL quality tier.
- The 14,362 Pixyz `Metadata` components and 432,925 properties were converted to one compact catalog plus one scene store.
- All 14,362 scene bindings validate, with zero null or unresolved bindings. All 224 unit colliders remain selectable.
- The serialized scene fell from 86,216,356 bytes to 54,749,041 bytes. Scene plus catalog is 74,295,207 bytes, saving 11,921,149 bytes (11.37 MiB, 13.83%).
- High-quality DXT release candidates built successfully with Brotli, hashed names, data caching, no decompression fallback, high managed stripping, and no player logging.

| Candidate | Reported bytes | WASM Brotli | Data Brotli | Build time | Warnings |
|---|---:|---:|---:|---:|---:|
| Disk Size LTO, final | 16,698,590 | 5,815,857 | 10,718,496 | 689.2 s | 0 |
| Runtime Speed LTO, comparison | 19,963,690 | 8,834,091 | 10,962,604 | 2,930.0 s | 0 |

The same-source preliminary builds measured Runtime Speed LTO at 13.89% larger than Disk Size LTO and roughly three times as slow to build. The final Disk Size build then removed Pixyz and Addressables from the WebGL player graph, reducing it by another 830,080 bytes to 16,698,590 bytes. The recorded Runtime Speed artifact predates that final dependency exclusion, so it is a conservative comparison rather than a byte-for-byte final-source comparison. Disk Size LTO remains the command-line default; use Runtime Speed LTO only if representative-device profiling demonstrates a material runtime gain.

## Milestone status

### M0 — reproducible measurements

Complete. `WebGLBuildPipeline` provides deterministic profiling, Disk Size LTO, and Runtime Speed LTO builds for DXT/ASTC, High/Low quality, and 512/1024/2048 sky variants. Every build emits a JSON manifest beside the player and under `Docs/WebGLBuildManifests`.

### M1 — payload and GPU cleanup

Complete for the source project and release path.

- WebGL is removed from the PC and Mobile quality tiers and assigned a dedicated WebGL tier.
- Build Settings contain only the optimized scene.
- TMP example resources and Modern UI editor-only catalogs were removed from runtime `Resources` inclusion while preserving GUIDs.
- The unused StreamingAssets database initializer was removed.
- Addressables references are excluded from WebGL player compilation; neither Addressables nor Pixyz remains in the final stripped WebGL assembly set.
- DXT/ASTC, High/Low, and sky-resolution A/B variants are exposed by the build pipeline.
- Two stale Cesium settings assets whose package scripts are absent were moved under an Editor-only quarantine, preserving their GUIDs and excluding them from player payloads.

### M2 — compact BIM metadata

Complete and applied to the optimized scene. The converter sorts source components deterministically, interns repeated strings, preserves duplicate keys and property order, validates every property before removal, creates one store, and writes a conversion report. Runtime hover/click metadata resolution no longer depends on Pixyz types.

### M3 — spatial clustering

The reversible one-floor prototype is implemented but intentionally not applied project-wide. Select a floor root and run the prototype menu command; it combines eligible non-unit renderers by spatial cell and material, creates triangle-to-element maps, and preserves unit selection. Full rollout remains gated by the plan's 15% representative-device gain and visual/collider validation requirements.

### M4 — interaction spikes and idle work

Complete at the code level.

- Metadata, resident, and unit-search rows are pooled instead of destroyed/recreated.
- Renderer highlighting uses `MaterialPropertyBlock` snapshots instead of cloning `Renderer.materials`.
- Unit renderers and shader IDs are cached.
- Wall fading is dirty-driven and skips unchanged property-block writes.
- Release logging is gated to Editor/Development builds.

### M5 — memory and delivery

Brotli delivery configuration, hashing, data caching, build manifests, and an HTTP-header smoke test are implemented. Heap sizes and spatial streaming are deliberately unchanged: the plan requires post-M1–M4 device profiling before tuning heap growth or adopting streaming. The final release `.data.br` payload is approximately 10.72 MB, so streaming is not justified from payload size alone.

## Validation evidence

- EditMode tests: 3 passed, 0 failed.
- Play Mode smoke: compact store loaded, representative metadata samples resolved, UI controllers were present, and the Console reported zero errors.
- Scene integrity: 1 store, 14,362 elements, 432,925 properties, 14,362 bindings, 0 null bindings, 0 invalid elements, 0 unresolved bindings, 0 Pixyz Metadata components, 224 unit colliders.
- Release builds: final Disk Size LTO succeeded with 0 errors and 0 warnings; Runtime Speed LTO comparison succeeded with 0 errors and 0 warnings.
- `git diff --check` has no errors in newly authored source; one blank-name whitespace line remains in Unity's optimized-scene serialization.

## Commands

See `Docs/WebGLBuildAndValidation.md` for menu paths, command-line arguments, build matrix, and deployment-header verification.
