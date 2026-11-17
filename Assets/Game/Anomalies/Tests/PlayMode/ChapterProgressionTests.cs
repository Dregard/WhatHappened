#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Game.Anomalies.Tests
{
    public sealed class ChapterProgressionTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Loads old puzzle/star fields through production JSON conversion without touching profile files or discarding preserved balances.</summary>
        [NUnit.Framework.Test]
        public void LegacyProfilePreservesGoldProgressAndAdaptedStock()
        {
            const string json = "{\"Version\":17,\"Profile\":{\"StarCurrency\":{\"CurrentAmount\":99},\"GoldCurrency\":{\"CurrentAmount\":731},\"GameplayProgress\":{\"LevelIndex\":8,\"CurrentAttempt\":3,\"HasCompletedFrozenPieceTutorial\":true,\"PuzzlePieces\":[1,2]},\"Boosters\":{\"BoosterCounts\":{\"Compass\":4,\"FreezeTimer\":2,\"MagicWand\":9,\"Refresh\":3},\"OpenedBoosters\":[2,3,4]}}}";
            var deserializer = Activator.CreateInstance(Find("Game.Profile.Serialization.DeserializationController"));
            var args = new object[] { json, null };
            Assert.That(Call(deserializer, "TryDeserializeProfileData", args), Is.True);
            var envelope = args[1];
            var profiles = (IDictionary)Get(envelope, "Profile");
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal(Find("Game.Profile.Signals+ProfileHolderAppeared")).OptionalSubscriber();
            var bus = container.Resolve<SignalBus>();
            var goldData = ScriptableObject.CreateInstance(Find("Game.Currency.GoldCurrencyData"));
            try
            {
                var gold = Activator.CreateInstance(Find("Game.Currency.GoldCurrencyController"), bus, goldData);
                var progress = Activator.CreateInstance(Find("Game.Gameplay.GameplayProgressController"), bus);
                var boosters = Activator.CreateInstance(Find("Game.Boosters.BoostersProfileController"), bus);
                foreach (var holder in new[] { gold, progress, boosters })
                {
                    var conversion = new[] { profiles[Get(holder, "ProfileName")], Get(holder, "ProfileType"), null };
                    Assert.That(Find("Game.Profile.ProfileUtils").GetMethod("TryConvertToProfile").Invoke(null, conversion), Is.True);
                    Call(holder, "ApplyProfile", conversion[2]);
                    profiles[Get(holder, "ProfileName")] = conversion[2];
                }
                Assert.That(Call(gold, "GetCurrencyValue"), Is.EqualTo(731));
                Assert.That(Get(progress, "CurrentLevel"), Is.EqualTo(9));
                Assert.That(Get(progress, "CurrentAttempt"), Is.EqualTo(3));
                var hint = Enum.Parse(Find("Game.Boosters.BoosterKind"), "Compass");
                var photo = Enum.Parse(Find("Game.Boosters.BoosterKind"), "FreezeTimer");
                Assert.That(Call(boosters, "GetBoosterCount", hint), Is.EqualTo(4));
                Assert.That(Call(boosters, "GetBoosterCount", photo), Is.EqualTo(2));
                Assert.That(Call(boosters, "IsOpened", hint), Is.True);
                Assert.That(Call(boosters, "IsOpened", photo), Is.True);
                var serializer = Activator.CreateInstance(Find("Game.Profile.Serialization.SerializationController"));
                var serialization = new[] { envelope, null };
                Assert.That(Call(serializer, "TrySerializeProfileData", serialization), Is.True);
                var roundTrip = new[] { serialization[1], null };
                Assert.That(Call(deserializer, "TryDeserializeProfileData", roundTrip), Is.True);
                Assert.That(((IDictionary)Get(roundTrip[1], "Profile")).Contains("StarCurrency"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(goldData);
                bus.LateDispose();
            }
        }

        /// <summary>Exercises actual chapter profile transitions, duplicate guards, serialization, HUD flight and investigation slots without application startup or external services.</summary>
        [UnityTest]
        public IEnumerator CluesScreensPersistenceAndLoop()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal(Find("Game.Profile.Signals+ProfileHolderAppeared")).OptionalSubscriber();
            container.DeclareSignal(Find("Game.Currency.Signals+CurrencyAdded")).OptionalSubscriber();
            var bus = container.Resolve<SignalBus>();
            var progress = Activator.CreateInstance(Find("Game.Gameplay.GameplayProgressController"), bus);
            var profile = Call(progress, "CreateAndApplyDefaultProfile");
            var data = ScriptableObject.CreateInstance(Find("Game.Levels.LevelsData"));
            var goldData = ScriptableObject.CreateInstance(Find("Game.Currency.GoldCurrencyData"));
            var reward = ScriptableObject.CreateInstance(Find("Game.Purchase.CurrencyRewardData"));
            var gold = Activator.CreateInstance(Find("Game.Currency.GoldCurrencyController"), bus, goldData);
            Call(gold, "CreateAndApplyDefaultProfile");
            var currency = Activator.CreateInstance(Find("Game.Currency.CurrencySystem"), bus, List("Game.Currency.ICurrencyController", gold));
            var processor = Activator.CreateInstance(Find("Game.Purchase.CurrencyRewardProcessor"), currency);
            var rewards = Activator.CreateInstance(Find("Game.Purchase.RewardSystem"), List("Game.Purchase.IRewardProcessor", processor));
            Set(reward, "_currencyKind", Enum.Parse(Find("Game.Currency.CurrencyKind"), "Gold"));
            Set(reward, "_amount", 37);
            var rewardList = List("Game.Purchase.RewardData", reward);
            var root = new GameObject("ChapterFixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f);
            GameObject windowRoot = null;
            try
            {
                var references = Array.CreateInstance(Find("Game.Levels.LevelReference"), 3);
                for (var index = 0; index < references.Length; index++)
                {
                    var reference = Activator.CreateInstance(Find("Game.Levels.LevelReference"));
                    Set(reference, "LevelId", "TestLevel" + index);
                    references.SetValue(reference, index);
                }
                Set(data, "Levels", references);
                var chapters = Array.CreateInstance(Find("Game.Levels.ChapterData"), 2);
                var first = Activator.CreateInstance(Find("Game.Levels.ChapterData"));
                var firstLevels = Array.CreateInstance(Find("Game.Levels.LevelReference"), 2);
                firstLevels.SetValue(references.GetValue(1), 0);
                firstLevels.SetValue(references.GetValue(0), 1);
                Set(first, "LevelsArray", firstLevels);
                Set(first, "RewardsList", rewardList);
                chapters.SetValue(first, 0);
                var second = Activator.CreateInstance(Find("Game.Levels.ChapterData"));
                var secondLevels = Array.CreateInstance(Find("Game.Levels.LevelReference"), 1);
                secondLevels.SetValue(references.GetValue(2), 0);
                Set(second, "LevelsArray", secondLevels);
                Set(second, "RewardsList", rewardList);
                chapters.SetValue(second, 1);
                Set(data, "ChaptersArray", chapters);
                Call(progress, "Configure", data);
                Assert.That(Get(progress, "ActualLevelIndex"), Is.EqualTo(1));
                Assert.That(Call(progress, "GetLevelIndexAtOffset", 1), Is.EqualTo(0));
                Assert.That(Call(progress, "GetLevelIndexAtOffset", 2), Is.EqualTo(2));
                Assert.That(Call(progress, "GetLevelIndexAtOffset", 3), Is.EqualTo(1));
                Assert.That(Call(progress, "TryClaimChapterReward"), Is.False);
                Assert.That(Call(progress, "StartNextChapter"), Is.False);

                var hud = root.AddComponent(Find("Game.Gameplay.Chapters.ChapterHudView"));
                var investigationClicks = 0;
                Call(hud, "Initialize", "Laboratory", (Action)(() => investigationClicks++));
                Call(hud, "Show", 0, 2, true);
                var counter = root.GetComponentInChildren<Button>();
                counter.onClick.Invoke();
                Assert.That(investigationClicks, Is.EqualTo(1));
                var chapterSystem = Activator.CreateInstance(Find("Game.Gameplay.Chapters.ChapterSystem"), progress, null, null, null, null);
                Set(chapterSystem, "_view", hud);
                Call(chapterSystem, "BeginCompletion");
                Assert.That(counter.interactable, Is.False);
                var blockedInvestigation = Call(Call(chapterSystem, "ShowInvestigation"), "GetAwaiter");
                Assert.That(Get(blockedInvestigation, "IsCompleted"), Is.True);
                Call(blockedInvestigation, "GetResult");
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(0));
                yield return null;
                Assert.That(Call(progress, "CollectClue"), Is.True);
                Assert.That(Call(progress, "CollectClue"), Is.False);
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(1));
                Assert.That(Get(progress, "IsChapterComplete"), Is.False);
                Assert.That(Get(progress, "ActualLevelIndex"), Is.EqualTo(1), "Pending victory resumes its completed level.");
                Call(hud, "Show", 1, 2, true);
                Assert.That(Get(root.GetComponentsInChildren(Find("TMPro.TMP_Text")).Single(text => text.name == "CluesValue"), "text"), Is.EqualTo("1/2"));

                // Use the production JSON serialization/conversion pipeline in memory, without writing the user's profile files.
                var envelope = Activator.CreateInstance(Find("Game.Profile.ProfileData"));
                ((IDictionary)Get(envelope, "Profile")).Add("GameplayProgress", profile);
                var serializer = Activator.CreateInstance(Find("Game.Profile.Serialization.SerializationController"));
                var serializationArgs = new[] { envelope, null };
                Assert.That(Call(serializer, "TrySerializeProfileData", serializationArgs), Is.True);
                var deserializer = Activator.CreateInstance(Find("Game.Profile.Serialization.DeserializationController"));
                var deserializationArgs = new[] { serializationArgs[1], null };
                Assert.That(Call(deserializer, "TryDeserializeProfileData", deserializationArgs), Is.True);
                var saved = ((IDictionary)Get(deserializationArgs[1], "Profile"))["GameplayProgress"];
                var conversionArgs = new[] { saved, Find("Game.Gameplay.GameplayProgressProfile"), null };
                Assert.That(Find("Game.Profile.ProfileUtils").GetMethod("TryConvertToProfile").Invoke(null, conversionArgs), Is.True);
                progress = Activator.CreateInstance(Find("Game.Gameplay.GameplayProgressController"), bus);
                Call(progress, "ApplyProfile", conversionArgs[2]);
                Call(progress, "Configure", data);
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(1));
                Assert.That(Get(progress, "IsLevelVictoryPending"), Is.True);
                Assert.That(Get(progress, "CurrentLevel"), Is.EqualTo(2));
                Call(progress, "AcknowledgeLevelVictory");
                Assert.That(Get(progress, "ActualLevelIndex"), Is.EqualTo(0));

                windowRoot = new GameObject("InvestigationFixture", typeof(RectTransform));
                windowRoot.transform.SetParent(root.transform, false);
                var window = windowRoot.AddComponent(Find("Game.Gameplay.Chapters.ChapterWindow"));
                var windowController = windowRoot.AddComponent(Find("Game.Gameplay.Chapters.ChapterWindowController"));
                Assert.That(windowRoot.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(Get(windowController, "CanCloseByReturn"), Is.False);
                var model = Activator.CreateInstance(Find("Game.Gameplay.Chapters.ChapterWindowModel"));
                Set(model, "PresentationData", data);
                Set(model, "Mode", Enum.ToObject(Find("Game.Gameplay.Chapters.ChapterWindowModeAlias"), 1));
                Set(model, "CluesArray", new[] { sprite, null });
                var closeCount = 0;
                Set(model, "CloseAction", (Action)(() => closeCount++));
                window.GetType().GetMethod("Setup", Flags, null, new[] { model.GetType() }, null).Invoke(window, new[] { model });
                var images = windowRoot.GetComponentsInChildren<Image>(true);
                Assert.That(images.Single(image => image.transform.parent.name == "Evidence0" && image.name == "Icon").sprite, Is.SameAs(sprite));
                Assert.That(images.Single(image => image.transform.parent.name == "Evidence1" && image.name == "Icon").enabled, Is.False);
                Assert.That(windowRoot.GetComponentsInChildren(Find("TMPro.TMP_Text")).Any(text => (string)Get(text, "text") == "You found 1/2 evidences"), Is.True);
                var close = windowRoot.GetComponentsInChildren<Button>().Single(button => button.name == "Close");
                close.onClick.Invoke();
                Assert.That(closeCount, Is.EqualTo(1));
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(1), "Opening a view cannot award a clue or reward.");

                var inactiveOwner = new GameObject("InactiveVictoryOwner");
                inactiveOwner.transform.SetParent(root.transform, false);
                inactiveOwner.SetActive(false);
                var victoryRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Windows/WinWindow/WinWindow.prefab"), inactiveOwner.transform);
                var victory = victoryRoot.GetComponent(Find("Game.UI.Windows.WinWindow.WinWindow"));
                var victoryModel = Activator.CreateInstance(Find("Game.UI.Windows.WinWindow.WinWindowModel"));
                var victoryContinues = 0;
                Set(victoryModel, "CurrentLevel", 2);
                Set(victoryModel, "ContinueAction", (Action)(() => victoryContinues++));
                victory.GetType().GetMethod("Setup", Flags, null, new[] { victoryModel.GetType() }, null).Invoke(victory, new[] { victoryModel });
                Assert.That(victoryRoot.transform.Find("Content/Reward"), Is.Null);
                Assert.That(victoryRoot.transform.Find("Content/Statistics/Combo"), Is.Null);
                Assert.That(victoryRoot.transform.Find("Content/ButtonsLayout/AdsButton"), Is.Null);
                Assert.That(Get(victoryRoot.GetComponent(Find("Game.UI.Windows.Controllers.WindowController")), "CanCloseByReturn"), Is.False);
                Call(victory, "Continue");
                Assert.That(victoryContinues, Is.EqualTo(1), "Chapter levels continue without currency projectiles or ad rewards.");
                Assert.That(Call(gold, "GetCurrencyValue"), Is.EqualTo(0));

                Assert.That(Call(progress, "CollectClue"), Is.True);
                Assert.That(Get(progress, "IsChapterComplete"), Is.True);
                Assert.That(Call(progress, "TryClaimChapterReward"), Is.False, "Last level victory must precede chapter victory.");
                Assert.That(Call(progress, "StartNextChapter"), Is.False);
                Call(progress, "AcknowledgeLevelVictory");
                Assert.That(Call(progress, "TryClaimChapterReward"), Is.True);
                Call(Call(Call(rewards, "GiveRewards", rewardList), "GetAwaiter"), "GetResult");
                if ((bool)Call(progress, "TryClaimChapterReward"))
                    Call(Call(Call(rewards, "GiveRewards", rewardList), "GetAwaiter"), "GetResult");
                Assert.That(Call(gold, "GetCurrencyValue"), Is.EqualTo(37), "Repeated claims cannot duplicate the configured chapter reward.");
                Assert.That(Call(progress, "StartNextChapter"), Is.True);
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(0));
                Assert.That(Get(progress, "ActualLevelIndex"), Is.EqualTo(2));
                Assert.That(Call(progress, "CollectClue"), Is.True);
                Call(progress, "AcknowledgeLevelVictory");
                Assert.That(Call(progress, "TryClaimChapterReward"), Is.True);
                Call(Call(Call(rewards, "GiveRewards", rewardList), "GetAwaiter"), "GetResult");
                Assert.That(Call(gold, "GetCurrencyValue"), Is.EqualTo(74));
                Assert.That(Call(progress, "StartNextChapter"), Is.True);
                Assert.That(Get(progress, "ActualLevelIndex"), Is.EqualTo(1));
                Assert.That(Get(progress, "ClueCount"), Is.EqualTo(0));
                Assert.That(Get(progress, "IsChapterRewardClaimed"), Is.False);
                Assert.That(Call(progress, "CollectClue"), Is.True);
                Call(progress, "AcknowledgeLevelVictory");
                Assert.That(Call(progress, "CollectClue"), Is.True);
                Call(progress, "AcknowledgeLevelVictory");
                Assert.That(Call(progress, "TryClaimChapterReward"), Is.True, "A new loop permits a new chapter reward.");
                Call(Call(Call(rewards, "GiveRewards", rewardList), "GetAwaiter"), "GetResult");
                Assert.That(Call(gold, "GetCurrencyValue"), Is.EqualTo(111));
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(data);
                Object.Destroy(goldData);
                Object.Destroy(reward);
                Object.Destroy(sprite);
                Object.Destroy(texture);
            }
            yield return null;
        }

        /// <summary>Builds typed production dependency lists for the reflection-based isolated fixture.</summary>
        private static IList List(string typeName, params object[] valuesArray)
        {
            var list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(Find(typeName)));
            foreach (var value in valuesArray)
                list.Add(value);
            return list;
        }

        /// <summary>Resolves Assembly-CSharp contracts without adding a dependency from the isolated test assembly.</summary>
        private static Type Find(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
        }

        /// <summary>Reads the production state or fixture field.</summary>
        private static object Get(object target, string name)
        {
            var field = target.GetType().GetField(name, Flags);
            return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Flags).GetValue(target);
        }

        /// <summary>Sets only isolated fixture configuration.</summary>
        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, Flags).SetValue(target, value);
        }

        /// <summary>Invokes a production method and exposes its original failure.</summary>
        private static object Call(object target, string name, params object[] argumentsArray)
        {
            try
            {
                return target.GetType().GetMethod(name, Flags).Invoke(target, argumentsArray);
            }
            catch (TargetInvocationException error)
            {
                throw error.InnerException;
            }
        }
    }
}
#endif
