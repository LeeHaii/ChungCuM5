# WebGL Milestone 4 Validation

Date: 2026-08-14

Scene: `Assets/Scenes/AltScene_Optimized.unity`

## Applied Changes

| Metric | Before | After |
| --- | ---: | ---: |
| GameObjects | 27,513 | 21,404 |
| Transform-only `Curve_2` leaves | 6,109 | 0 |
| MeshColliders | 14,342 | 14,290 |
| Distinct readable meshes | 582 | 580 |

- Added `Selectable`, `BimInspectable`, and `CameraCollision` layers through Unity APIs.
- Assigned 224 apartment units, 20,700 BIM inspection objects, and 60 camera collision objects.
- Removed 2 purely visual MeshColliders.
- Replaced 50 camera wall MeshColliders with BoxCollider proxies.
- Preserved 7 negative-scale and 3 meshless camera wall MeshColliders because BoxCollider geometry would be invalid or could not be derived safely.
- Disabled Read/Write on the audited `Oak_Tree.fbx` and `Poplar_Tree.fbx` importers.

## Selection Guardrail

14,280 MeshColliders remain on apartment and BIM inspection targets. The current selection path resolves metadata from the hit object or its immediate parent, so reducing these colliders below 500 would break per-element BIM selection without a grouped proxy-to-element ID mapping. That mapping belongs with the compact metadata migration in Milestone 6.

## Runtime Verification

- Unity compiled the editor tooling with zero errors.
- Play Mode completed with zero runtime errors after negative-scale proxy repair.
- All 224 apartment units retained colliders.
- All 60 camera wall objects retained valid collision: 50 BoxColliders and 10 MeshColliders.
- The runtime center-screen selection ray hit a `BimInspectable` MeshCollider and resolved metadata.
- Representative IFC wall, window, door, and floor colliders all passed direct raycast checks.
- `OrbitCamera` uses only the `CameraCollision` mask (bit value 256).
- `HoverManager` uses `Selectable`, `BimInspectable`, and `CameraCollision` (bit value 448).
- Idle profiler counters reported zero physics queries and no physics simulation workload of consequence in the sampled frame.
- The Game view and application UI rendered normally in the standard overview.

The only remaining console warning was the pre-existing UnitySkills port self-test warning.
