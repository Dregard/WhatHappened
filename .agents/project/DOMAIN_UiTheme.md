# UI theme and mockup screens

## Ownership
- `Assets/Game/Anomalies/Theme/UiThemeData.cs` holds every mockup sprite, the font and text/veil colors. The asset is `Assets/Game/Anomalies/Theme/Resources/UiThemeData.asset`, loaded through `UiThemeData.LoadOrDefault()`. Art copies live in `Assets/Game/Anomalies/Theme/Art/`; `Assets/TempAssets/` is only the delivered import source.
- `Assets/Game/UI/Editor/UiThemeBuilder.cs` (menu `Game/UI/Build Theme Data`) copies art, sets sprite import/nine-slice borders and fills the theme. Rerun it after adding art.
- `Assets/Game/Anomalies/Theme/UiKit.cs` builds themed primitives positioned in 1080x1920 mockup pixels (origin top-left) as normalized anchors. The delivered art carries transparent shadow margins: `PlacePadded` with `BodyPad`, `FramePad`, `ButtonPad`, `RibbonPad` keeps the visible edge on the mockup rectangle. Bold lettering uses `UiThemeData.BoldText`, outline and `BoldCharacterSpacing`.
- No notepad icon was delivered: `ClueCounterIcon` stays empty until art exists. `Daily_Btn.png` is not imported (no daily feature in the prototype). The mockup banner placeholder is not drawn; the real banner is owned by the Ads domain.

## Screens
- Search HUD: `Assets/Game/Anomalies/AnomalySearchView.cs` (background, board frame, polaroid tilted 4.6 degrees clockwise with thumbnail/pin/tape, shown only after Ready; stage label, task and narrative plates, slots, tap-to-start prompt, ORIGINAL tape tilted over the board), `Assets/Game/Anomalies/AnomalyBoosterView.cs` (photo overlay with 5 s countdown, hint finger and pulsing target ring), `Assets/Game/Gameplay/Chapters/ChapterHudView.cs` (location title, evidence counter that opens the investigation), `Assets/Game/UI/Hud/HudLayoutView.cs` (gold, Hint and Photo widgets; the photo widget appears once the search has started). `HudView` hides the prefab's old counters; the timer and pause button appear only when a level has `LevelTime > 0`.
- Chapter screens: `Assets/Game/Gameplay/Chapters/ChapterWindow.cs` renders the investigation and the result sequence (evidence found -> level reward, or all evidences -> culprit reveal -> culprit scene -> chapter reward). `ChapterSystem` supplies the model and claim/commit callbacks; `GameplaySystem` supplies continuation. The old `WinWindow` remains only for legacy non-chapter progression.
- Out-of-stock popup: `Assets/Game/UI/Windows/BoosterWindow/BoosterWindow.cs` builds the themed popup over the prefab and reuses its buy/ad/close actions.
- Photo stock is per level: `FreezeTimerBoosterData.PhotosPerLevel` refills it at each level start (`BoosterSystem.RefillPerLevelBoosters`); purchases add during the level and leftovers do not carry over.

## Authoring data
- `Assets/Game/Anomalies/AnomalyLevelData.cs`: `ClueImage` (evidence icon), `ClueName`, `IntroText`. `Assets/Game/Levels/ChapterData.cs`: `Title`, `LevelRewardsList`, `RewardsList`, culprit portrait/caption, scene image/caption. Amounts live only in assets; mockup numbers are illustrative. Result timings are in `Assets/Game/Levels/LevelsData.cs`.
- The final level of a chapter skips the level reward and goes to the culprit sequence; the chapter reward uses the existing claim flag.

## Visual verification
- The 1080x1920 mockups were compared with screenshots rendered from the production views with fake data (disposable harness, removed after use). Play mode freezes while the editor is unfocused unless `Application.runInBackground` is set, and `WaitForEndOfFrame` never fires then; render a camera into a RenderTexture instead.
- Managed `run_tests` for PlayMode returned an unknown outcome twice (transport lost around the play-mode transition); results were not recovered.
