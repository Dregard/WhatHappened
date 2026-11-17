# Slicer memory task: completed verification

## Contract and implementation
2048×2048 defaults now pass at exactly 12, 50 and 64 pieces without reducing roundness or other quality settings, changing generated contours, or changing the puzzle save schema. Existing unrelated user changes were preserved. Architecture and ownership are documented in .agents/project/DOMAIN_PuzzleAuthoring.md.

- Assets/Game/Puzzles/Editor/PuzzleSlicerTextureUtility.cs retains RGBA32 polygon-bounds crops with one transparent sampling-gutter pixel and Clamp wrapping. Borrowed Color32 views transfer ownership; working creation copies source bytes without a PNG round trip or full-size float buffers.
- Assets/Game/Puzzles/Editor/PuzzleSlicerSessionController.cs restores crop offsets, releases retired textures, and supports AddPolygon(List<Vector2>, bool applyWorkingTexture=true). Generation and Load upload the working image once per batch.
- Assets/Game/Puzzles/Editor/PuzzleSlicerGenerationController.cs keeps the 64*1024*1024 staged-texel guard, counts actual crops plus padded working area, checks device dimensions, stages and validates pieces/coverage, reserves destination capacity before freeing old markup, and releases staging on rejection.
- Assets/Game/Puzzles/Editor/PuzzlePieceMeshBuilder.cs translates extracted crop contours before simplification and retains padded centering/UV dimensions. Existing extraction, triangulation and border geometry are reused.
- Assets/Game/Puzzles/Editor/PuzzleSlicerCanvasView.cs positions crop previews; Assets/Game/Puzzles/Editor/PuzzleSlicerLayersView.cs releases deleted textures.
- Assets/Game/Puzzles/Editor/PuzzleSlicerPersistenceController.cs passes crop/padded geometry data and releases the temporary background source.
- Assets/Game/Puzzles/Editor/PuzzleSlicerSessionModel.cs removes an unused PNG cache.
- Assets/Game/Puzzles/Editor/PuzzleSlicerEditor.cs delegates source-change clearing to the shared session path. Preserve its substantial pre-existing refactor.

## Root causes
The padded image is 2448² = 5,992,704 texels. Old retention was (count+1) full-size textures: 77,905,152 / 305,627,904 / 389,525,760 staged texels at 12/50/64, exceeding 67,108,864. Each old cut also allocated two full-size float Color arrays, together 191,766,528 bytes.

A separate Load round-trip discrepancy was reproduced at piece index 31, pixel (1835.5,506.5): the scalar unit-to-pixel expression changed classification, while a rounded Vector2 multiply followed by Vector2 origin addition did not. The initial vector-based precision diagnostic missed this scalar-expression distinction. Assets/Game/Puzzles/Editor/PuzzleSlicerSessionController.cs now uses the vector operations; all integrated ownership hashes and exact rebaked meshes pass. Saved formats, rasterizer and geometry algorithms remain unchanged.

## Final measurements
Windows Unity Editor, readable/uncompressed RGBA source PNG, no mips, default generator settings except count. Figures are bytes unless marked texels.

| Count | Crop texels | Retained crop native | Working native | Max validation mesh native | Pixel-buffer upper bound | Managed frame allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 12 | 4,712,732 | 39,398,020 | 50,253,610 | 79,140 | 35,628,144 | 51,693,803 |
| 50 | 4,848,296 | 43,128,594 | 50,253,610 | 79,140 | 36,170,400 | 67,628,524 |
| 64 | 4,796,109 | 43,156,476 | 50,253,610 | 71,954 | 35,961,652 | 73,696,748 |

Native texture-plus-validation-mesh bounds: 89,730,770 / 93,461,344 / 93,482,040. Retained texture totals exclude that temporary mesh. Pixel-buffer bound is 4*(source texels + sum crop texels). Managed GC Allocated In Frame is a conservative allocation bound for the measured generation frame, not an exact live managed peak; it includes those managed buffers, so do not add them twice. GC.GetAllocatedBytesForCurrentThread returned zero under Mono and was discarded.

Native figures use Profiler.GetRuntimeMemorySizeLong; they exclude original source/editor baseline and unmeasured driver transients. These are incremental resource bounds, not whole-process peak measurements. Transactional replacement also retains the previous session until commit; add its retained resources to the staging bound. No separate full-source-sized texture is retained per piece.

## Verification evidence
Final source compiled successfully; six existing unrelated warnings remain (Assets/TrayGestureVerification/Editor/TrayGestureVerification.cs, Assets/Game/Levels/Editor/LevelChooserEditor.cs, and a pre-existing temporary baseline). Test-only compilation also passed with zero warnings.
- SlicerMemoryVerification.SlicerMemoryTests.Generate12 / Generate50 / Generate64: each passed exact count, every opaque source coordinate and RGBA, no duplicate ownership, deterministic/default generator contours, staged budget and actual BAKE.
- SlicerMemoryVerification.SlicerMemoryTests.Load2048EditAndRebake(12/50/64): each passed actual disk Load, per-piece global ownership hash, source coverage/colors, delete/re-add bytes, reorder/anchor edit, BAKE, exact saved-vs-rebaked vertices and UVs, and repeated Load.
- SlicerMemoryVerification.SlicerMemoryTests.CanvasDisplaysCrops: final source passed real isolated editor-window repaint; window closed and prior focus restored.
- Previously passed unchanged-path evidence: CroppedGeometryMatchesFullSizeAndMargins (exact contour/vertices/triangles/UV0/UV1/colors vs full-size, including gutters); BudgetRejectionKeepsMarkup (6000×6000 genuine budget rejection); FailedGenerationIsTransactional (invalid settings and staged empty piece preserve old layers/input); PuzzleAutomaticVerification.PuzzleAutomaticSlicingTests.ParametersControlTheirCharacteristics.
- Earlier Load50 failure was diagnosed and corrected; it is not an outstanding failure. Assertions were not weakened. No network checks or PlayMode were needed or run.

## Recovery and temporary artifacts
Interrupted old batch had no pending receipt after recovery, editor ready with zero active operations; saved metric/fixture files were inspected before fresh narrow tests. Old receipt 62b3e11f-32cd-4eaa-bb48-59c351f7af04 was already confirmed inactive with a runner timeout and was not replayed.

Temporary Unity scope a03e634461c643648687794cf314f8c3 at Assets/__OrchestratorTemp-a03e634461c643648687794cf314f8c3 was sealed for orchestrator cleanup (75 files) after readiness. Do not edit or shell-delete sealed contents. Its tests are disposable, reflection-based EditMode checks; no scenes or network peers. Disk fixtures require synchronous import and unloading cached PuzzleData before reread. Tests flush/clear their own Undo records after Bake so fixture root records survive test isolation/reload; older empty-root fixtures are invalid test artifacts, not accepted evidence.

Final editor: clean EditMode, active Assets/LionStudios/AnimatedLionLogoScene.unity, same root count 3, no scene modifications. No new reusable skill was created: the temporary verification harness remains task-specific; durable contracts and the precision caveat are in .agents/project/DOMAIN_PuzzleAuthoring.md.
