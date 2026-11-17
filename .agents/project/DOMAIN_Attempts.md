# Search attempt time and results

- `Assets/Game/Levels/LevelController.cs` owns `Assets/Game/Levels/LevelAttemptTimeModel.cs`, countdown, timeout and external pause. `InitializeSearch` resets the attempt; Ready starts ticks. Initialization, fully found-stage presentation, photo, gameplay pause, timeout waiting and completed attempts do not advance active time. Untimed levels still accumulate active time.
- Countdown freeze remains separate from gameplay pause. Photo does not change either flag; closing it cannot unpause an independent requester. See [.agents/project/DOMAIN_Boosters.md](.agents/project/DOMAIN_Boosters.md).
- `Assets/Game/Gameplay/LevelsManager.cs` marks extra-time receipt through `HandleExtraTimeBought`, shared by paid/rewarded continuation in `Assets/Game/UI/Windows/TimeLoseWindow/TimeLoseWindowModelProcessor.cs`. Continuation preserves found anomalies and photo usage; a fresh attempt clears them.
- `Assets/Game/Levels/LevelAttemptStatisticsModel.cs` snapshots elapsed seconds, formatted minutes/seconds and time eligibility. Puzzle counts, combos and combo eligibility were removed.
- `Assets/Game/Gameplay/GameplaySystem.cs` captures the completed attempt after search completion. `Assets/Game/UI/Windows/WinWindow/WinWindow.cs` displays time and an explicit continuation button; it contains no individual rewards, ads or puzzle statistics.
- Chapter victory follows the final level victory, with chapter rewards and cyclic reset; see [.agents/project/DOMAIN_Chapters.md](.agents/project/DOMAIN_Chapters.md).
- Retained focused production-prefab regression: `Assets/Game/Anomalies/Tests/PlayMode/AnomalyAttemptTests.cs` covers Ready, actual timer frames, pause, timeout, continuation and unload using isolated runtime objects.
