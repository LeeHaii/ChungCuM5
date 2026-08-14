# BIM WebGL Cleanup Dry Run

Generated: 2026-08-14 10:32:30 UTC

Scene: `Assets/Scenes/AltScene_Optimized.unity`

No scene objects or assets were modified by this analysis.

## Counts

| Metric | Count |
| --- | ---: |
| GameObjects | 21,404 |
| Active GameObjects | 21,301 |
| Renderers | 14,734 |
| Triangles | 1,388,069 |
| Material slots | 18,027 |
| Transform-only leaves | 2 |
| Transform-only `Curve_2` leaves | 0 |
| MeshColliders | 14,290 |
| MeshColliders on non-selectable objects | 10 |
| Purely visual MeshColliders eligible for removal | 0 |
| BackgroundWall MeshColliders | 10 |
| BackgroundWall MeshColliders eligible for BoxCollider proxies | 0 |
| Negative-scale BackgroundWall BoxColliders requiring repair | 0 |
| Pixyz Metadata components | 14,362 |
| Unit-tagged objects | 224 |
| BackgroundWall-tagged objects | 60 |
| Planned Selectable layer assignments | 0 |
| Planned BimInspectable layer assignments | 0 |
| Planned CameraCollision layer assignments | 0 |
| Distinct readable meshes | 580 |
| Readable meshes still required by MeshCollider | 537 |
| Model assets safe to disable Read/Write | 0 |
| Repeated mesh/material groups | 417 |
| Instances in repeated groups | 14,568 |
| Objects without static flags | 21,348 |
| Potential collider proxies | 6,398 |

## Selection Guardrail

14,280 MeshColliders are attached to apartment or BIM inspection targets. They remain because runtime metadata selection currently resolves the hit object or its immediate parent; grouped proxies require an element-ID mapping before these colliders can be removed safely.

## Top Repeated Mesh/Material Combinations

| Mesh | Materials | Instances |
| --- | --- | ---: |
| Mesh_2671503 | Color #d29f5fff, Color #764633ff | 1,092 |
| Mesh_2664629 | Color #e0b27eff, Color #0080c019 | 596 |
| Mesh_2598706 | Color #0080c03f, Color #404040ff, Color #fbfbfbff, Color #0e0e10ff | 340 |
| Mesh_2464151 | Color #f9f9f9ff | 243 |
| Mesh_2482090 | Color #f7f7f7ff | 234 |
| Mesh_2504420 | Color #f7f7f7ff | 234 |
| Mesh_2700125 | Color #d29f5fff, Color #764633ff | 226 |
| Cube | BlueTransparent | 224 |
| Mesh_2659273 | Color #f7f7f7ff | 210 |
| Mesh_2500531 | Color #e0b27eff, Color #0080c019 | 198 |
| Mesh_2694003 | Color #f7f7f7ff | 180 |
| Mesh_2466649 | Color #f9f9f9ff | 135 |
| Mesh_2671471 | Color #e0b27eff, Color #0080c019 | 110 |
| Mesh_2465376 | Color #808080ff | 108 |
| Mesh_2465927 | Color #f9f9f9ff | 108 |
| Mesh_2466591 | Color #f9f9f9ff | 108 |
| Mesh_2474982 | Color #808080ff | 108 |
| Mesh_2478027 | Color #808080ff | 107 |
| Mesh_2466668 | Color #f9f9f9ff | 106 |
| Mesh_2475159 | Color #f9f9f9ff | 84 |
| Mesh_2465358 | Color #f9f9f9ff | 81 |
| Mesh_2466118 | Color #f9f9f9ff | 81 |
| Mesh_2468381 | Color #808080ff | 81 |
| Mesh_2468409 | Color #808080ff | 81 |
| Mesh_2468553 | Color #f9f9f9ff | 81 |

## Safe Read/Write Disable Candidates

(none)
