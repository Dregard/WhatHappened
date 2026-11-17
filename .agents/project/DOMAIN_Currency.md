# Currency and rewards

- `Assets/Game/Initialization/GameplayInstaller.cs` registers only `Assets/Game/Currency/GoldCurrencyController.cs` behind `Assets/Game/Currency/CurrencySystem.cs`. Star currency, its assets, rewards and HUD/cheat counters were removed. Numeric currency value 2 stays reserved in `Assets/Game/Currency/CurrencyKind.cs`.
- Gold profile identity, prices, purchase-time continuation and the common reward system are retained. Chapter rewards are authored in `Assets/Game/Levels/ChapterData.cs` and processed through `Assets/Game/Purchase/RewardSystem.cs`; see [.agents/project/DOMAIN_Chapters.md](.agents/project/DOMAIN_Chapters.md).
- `Assets/Game/Profile/Serialization/DeserializationController.cs` and `Assets/Game/Profile/ProfileUtils.cs` tolerate unknown profile sections/fields. Old star data can remain in the serialized envelope without a registered star controller. `Assets/Game/Anomalies/Tests/PlayMode/ChapterProgressionTests.cs` contains the legacy JSON compatibility regression preserving gold, progression and adapted booster stock.
- `Assets/Game/UI/Windows/Elements/CurrencyCounter.cs` and gold counters remain shared infrastructure. Combo grants, combo notifications and per-level reward assets are removed.
- Decorative finding stars belong to `Assets/Game/Anomalies/AnomalyFindingEffects.cs`; their animation has no currency or reward side effects.
