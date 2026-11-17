# Automatic slicing performance and angular quality

Read when profiling automatic slicing or comparing the angular generator. Contracts: [.agents/project/DOMAIN_PuzzleAutomaticSlicing.md](.agents/project/DOMAIN_PuzzleAutomaticSlicing.md). Focused checks: [.agents/project/VALIDATION_PuzzleAutomaticSlicing.md](.agents/project/VALIDATION_PuzzleAutomaticSlicing.md).

## Reproducible comparison

Measured on Windows 10.0.26200 in the same open Unity 6000.3.9f1 editor, using its Mono runtime. Each table entry is one completed synchronous TryGenerate call, not a statistical median. Timing includes preflight, candidate search, native texture/mesh formation, validation and commit; source import and subsequent independent test assertions are excluded. No source image, puzzle asset or existing authoring window was replaced.

Both versions used PieceCount=16, Seed=11, RandomSeed=false, ContourComplexity=1, SizeSpread=0, PreferredMaxAspectRatio=3, Roundness=0, CornerSpread=0.6, SideSpread=0.6 and PPU=100. These are explicit benchmark settings, not an inference about the user's existing window controls.

| Source | Before (seconds) | Final (seconds) | Speedup |
| --- | ---: | ---: | ---: |
| Opaque RGBA32 512×512 synthetic source | 61.524865 | 4.961060 | 12.4× |
| Imported copy of `Assets/Game/Levels/Sprites/2_FT.png`, 2048×2048 | 300.212683 | 36.099726 | 8.3× |

The synthetic source uses `Assets/Game/Puzzles/Tests/Editor/PuzzleSlicerTestFixture.cs` Source(false, 512): Color32((byte)x, (byte)y, 93, 255). The imported PNG copy was readable, uncompressed, without mipmaps, alphaIsTransparency=false and NPOT scaling=None; original import settings were untouched. It contains 2,603,094 nonzero-alpha pixels. Original PNG SHA256: B20A4512D7FFED6821346ACF2CB206A083D8A1462F77B341B5A35DA89C1CFA07.

The baseline was a snapshot of the task-start editor sources, including pre-existing uncommitted angular changes. It was compiled into a separate netstandard2.1 managed assembly referencing the same Unity/editor dependencies, loaded in the existing editor, and invoked on isolated models. Production and snapshot controllers followed the same complete native transaction. Only Stopwatch/Console instrumentation was added to the snapshot. The final version used the actual imported production assembly.

The baseline 2048 call returned true and recorded 300.212683 seconds, but its NUnit wrapper exceeded the framework's 180-second per-test limit. Its managed runner also returned a 120-second timeout. The completed XML established the wrapper timeout, not a geometry pass. Do not reuse it as baseline correctness evidence or replay that operation. Both final timing cases passed, including independent exact Color32/ownership and four-neighbor connectivity checks after the timer stopped. Final timing comparisons therefore use an observed completed baseline call, not an inferred timeout duration.

## Causes and changes

- Axis-aligned recursive fine cells naturally produce orthogonal stair sequences after graph growth. Coherent displacement alone inclines them while retaining many near-right turns.
- A disconnected raster tip previously halved displacement across the entire graph. This flattened unrelated long seams and often forced the unchanged sixteen-candidate shape search to continue.
- Instrumented native baseline calls spent 307.141 of 339.264 candidate-construction seconds in refinement (90.5%, 25 candidates across both sources). Edge extraction, growth, angular validation and final mesh validation were secondary. Repeated component floods, relabeling and bounds rescans on already filled rectangular cells dominated.
- `Assets/Game/Puzzles/Editor/PuzzleSlicerPolygonRegionModel.cs` now directly splits a provably filled rectangle; clipped regions keep the general component path.
- `Assets/Game/Puzzles/Editor/PuzzleSlicerAngularBoundaryBuilder.cs` adds shared seeded local angular variation and tries alternate local streams. Connectivity failures pin a nearby movable vertex before global clearance is reduced.
- `Assets/Game/Puzzles/Editor/PuzzleSlicerAngularPixelValidator.cs` keeps strict raster coverage/connectivity and reuses its flood queue. Final float mesh validation, candidate count, aspect/size preferences, side ceiling and rounding controls were not weakened.

Before native execution, bounded standalone .NET 8 probes invoked the actual managed geometry with an immutable PNG-alpha prefix snapshot and Unity managed math types, without a Unity instance, textures, sockets or peers. This established bounded input costs; it is separate from native texture, mesh and persistence evidence.

## Shape and compatibility evidence

`Assets/Game/Puzzles/Tests/Editor/PuzzleSlicerAngularGenerationTests.cs` includes permanent 512-square/16-piece unrounded regressions over seeds 11/17/31. Final native results:

| Seed | Shared seam length inclined ≥10° from both axes | Internal turns within 10° of 90° |
| --- | ---: | ---: |
| 11 | 63.1% | 26.8% |
| 17 | 60.4% | 24.3% |
| 31 | 55.8% | 26.8% |

The native baseline 512-square/seed11 result had 62.6% near-right internal turns. Before/final contour renders on this identical input were visually inspected: final seams have varied directions and turn angles without fillet samples. Existing 48-square/six-piece seeds 11/17/31 produced 129/146/152 genuine corners; all keep the unchanged 60-side ceiling.

Final production passed 65 focused native cases: angular9, shape/preferences25, constructive/topology15, final mesh ownership5, independent rounding1, imported persistence/manual/legacy8 and complete timing2. One constructive test still equated straight with axis-aligned edges; that obsolete expectation was replaced by ≥30% visibly inclined seam length plus the unchanged side budget, preserving its complexity, concavity and exact common-edge assertions. Its other fourteen cases passed unchanged; the corrected case then passed.

Imported BAKE/Load/rebake covered opaque/disconnected-with-holes sources at zero/full rounding, alpha1/2/127/255, PPU100/37 and the 128-square/12-piece/full-spread regression. Exact selection/mesh arrays, Color32 ownership, connectivity, authored names/visibility/anchors, visual reorder, piece/background geometry/UV/color arrays and mesh subasset counts passed. Manual cutting and unit-only legacy Load/rebake passed separately.

The existing `Assets/LionStudios/AnimatedLionLogoScene.unity` scene and authoring windows were preserved; checks used isolated models and registered disposable scopes. No puzzle data format, serialized control, source image, manual-cut algorithm or runtime gameplay code changed. These editor-only checks require no PlayMode or multiplayer verification.
