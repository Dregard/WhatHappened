#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Game.Anomalies.Tests
{
    public sealed class AnomalyBoosterTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private GameObject _root;
        private GameObject _lifecycleRoot;
        private GameObject _eventRoot;
        private GameObject _hudRoot;
        private object _level;
        private object _levelData;
        private object _progress;
        private object _system;
        private object _hintKind;
        private object _photoKind;
        private AnomalyLevelData _data;
        private AnomalySearchView _view;
        private AnomalySearchSystem _search;
        private SignalBus _bus;
        private Texture2D _texture;
        private Sprite _sprite;

        /// <summary>Builds the production level/economy with local profiles and no application startup, SDKs or transport activity.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Levels/LevelController.prefab"));
            _level = _root.GetComponent(Find("Game.Levels.LevelController"));
            Set(_level, "_timeoutWarningTimes", new List<float>());
            _progress = Activator.CreateInstance(Find("Game.Levels.LevelProgressController"));
            _texture = new Texture2D(20, 10);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 20, 10), Vector2.one * 0.5f);
            _levelData = ScriptableObject.CreateInstance(Find("Game.Levels.LevelData"));
            Set(_levelData, "LevelTime", 60);
            _data = new AnomalyLevelData { MainImage = _sprite, FindingDuration = 0, TransitionDuration = 0 };
            _data.Presentation.PhotoDuration = 0;
            _data.Presentation.BoosterPhotoFlightDuration = _data.Presentation.BoosterPhotoLabelsFadeDuration = 0;
            _data.Presentation.FadeDuration = _data.Presentation.FlightDuration = _data.Presentation.StarsDuration = 0;
            _data.Presentation.BlackoutDuration = 0;
            _data.AnomaliesList.Add(new AnomalyData { Id = "a", Image = _sprite, Bounds = new Rect(0.1f, 0.1f, 0.2f, 0.2f) });
            _data.AnomaliesList.Add(new AnomalyData { Id = "b", Image = _sprite, Bounds = new Rect(0.6f, 0.6f, 0.2f, 0.2f) });
            _data.AnomaliesList.Add(new AnomalyData { Id = "c", Image = _sprite, Bounds = new Rect(0.4f, 0.1f, 0.2f, 0.2f) });
            _data.StagesList.Add(new AnomalyStageData { Count = 2, AnomalyIdsList = new List<string> { "a", "b" } });
            _data.StagesList.Add(new AnomalyStageData { Count = 1, AnomalyIdsList = new List<string> { "c" } });
            Set(_levelData, "SearchData", _data);
            PrepareAttempt();
            _eventRoot = new GameObject("BoosterTestInput", typeof(EventSystem));
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal(Find("Game.Profile.Signals+ProfileHolderAppeared")).OptionalSubscriber();
            container.DeclareSignal(Find("Game.Boosters.Signals+BoosterUsed")).OptionalSubscriber();
            _bus = container.Resolve<SignalBus>();
            var profile = Activator.CreateInstance(Find("Game.Boosters.BoostersProfileController"), _bus);
            Call(profile, "CreateAndApplyDefaultProfile");
            var gameplay = Activator.CreateInstance(Find("Game.Gameplay.GameplayProgressController"), _bus);
            Call(gameplay, "CreateAndApplyDefaultProfile");
            _lifecycleRoot = new GameObject("BoosterTestLifecycle");
            var lifecycle = _lifecycleRoot.AddComponent(Find("Game.Utils.UnityLifecycleSystem"));
            var manager = Activator.CreateInstance(Find("Game.Gameplay.LevelsManager"),
                null, _progress, null, _bus, container, gameplay, null, lifecycle, _level, null);
            var hintData = AssetDatabase.LoadAssetAtPath("Assets/Game/Boosters/Compas/CompassBoosterData.asset", Find("Game.Boosters.CompassBoosterData"));
            var photoData = AssetDatabase.LoadAssetAtPath("Assets/Game/Boosters/FreezeTimer/FreezeTimerBoosterData.asset", Find("Game.Boosters.FreezeTimerBoosterData"));
            var hint = Activator.CreateInstance(Find("Game.Boosters.CompassBooster"), manager, hintData, null);
            var photo = Activator.CreateInstance(Find("Game.Boosters.FreezeTimerBooster"), manager, photoData, null);
            var boosters = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Find("Game.Boosters.Booster")));
            boosters.Add(hint);
            boosters.Add(photo);
            _system = Activator.CreateInstance(Find("Game.Boosters.BoosterSystem"), profile, boosters, null, _bus, gameplay, _progress);
            _hintKind = Enum.Parse(Find("Game.Boosters.BoosterKind"), "Compass");
            _photoKind = Enum.Parse(Find("Game.Boosters.BoosterKind"), "FreezeTimer");
            Call(profile, "OpenBooster", _hintKind);
            Call(profile, "OpenBooster", _photoKind);
            Call(_system, "Add", _hintKind, 5);
            Call(_system, "Add", _photoKind, 5);
        }

        /// <summary>Releases only fixture-owned objects; no scene, production asset or saved profile is modified.</summary>
        [TearDown]
        public void TearDown()
        {
            if (_level != null)
                Call(_level, "ClearLevel");
            Object.DestroyImmediate(_hudRoot);
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_lifecycleRoot);
            Object.DestroyImmediate(_eventRoot);
            Object.DestroyImmediate((Object)_levelData);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
            _bus?.LateDispose();
        }

        /// <summary>Checks real one-time spending, five-second auto-close, pause ownership, hard attempt cap and reset across stages/restart.</summary>
        [UnityTest]
        public IEnumerator PhotoInventoryClockPauseLimitAndReset()
        {
            Assert.That(Use(_photoKind), Is.False, "Ready gate must preserve stock.");
            yield return StartAttempt();
            Assert.That(Use(_photoKind), Is.True);
            Assert.That(Count(_photoKind), Is.EqualTo(4));
            Assert.That(Use(_photoKind), Is.False);
            Assert.That(Count(_photoKind), Is.EqualTo(4));
            Assert.That(Photo().activeSelf, Is.True);
            Assert.That(Photo().GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(Photo().transform.Find("PhotoArea/Original").GetComponent<Image>().sprite, Is.SameAs(_sprite));
            var timer = (float)Get(_level, "TimerValue");
            yield return new WaitForSeconds(0.1f);
            Assert.That((float)Get(_level, "TimerValue"), Is.EqualTo(timer));
            Set(_level, "IsTimerPaused", true);
            _view.Boosters.Tick(20);
            yield return null;
            Assert.That(_view.Boosters.IsPhotoOpen, Is.True, "External pause must stop photo expiry.");
            Set(_level, "IsTimerPaused", false);
            var start = Time.time;
            while (_view.Boosters.IsPhotoOpen && Time.time - start < 6)
                yield return null;
            Assert.That(_view.Boosters.IsPhotoOpen, Is.False);
            Assert.That(Time.time - start, Is.InRange(4.7f, 5.2f));
            Assert.That(Photo().activeSelf, Is.False);
            yield return null;
            Assert.That((float)Get(_level, "TimerValue"), Is.LessThan(timer));
            Assert.That(Use(_photoKind), Is.True);
            _view.Boosters.Tick(5);
            Assert.That(Use(_photoKind), Is.True);
            Call(_level, "FreezeTimer");
            _view.Boosters.Tick(5);
            Assert.That((bool)Get(_level, "IsTimerPaused"), Is.True, "Closing photo must preserve another countdown freeze.");
            Call(_level, "UnfreezeTimer");
            Assert.That(Count(_photoKind), Is.EqualTo(2));
            Call(_system, "Add", _photoKind, -2);
            Assert.That(Use(_photoKind), Is.False, "An empty photo stock rejects the effect.");
            Call(_system, "Add", _photoKind, 2);
            _search.TryFind(_data.AnomaliesList[0].Bounds.center);
            _search.TryFind(_data.AnomaliesList[1].Bounds.center);
            yield return NextStage();
            Assert.That(_search.StageIndex, Is.EqualTo(1));
            Assert.That(_view.Boosters.PhotoUses, Is.EqualTo(3));
            Call(_level, "ClearLevel");
            PrepareAttempt();
            yield return StartAttempt();
            Assert.That(_view.Boosters.PhotoUses, Is.Zero);
            Assert.That(Use(_photoKind), Is.True);
            Assert.That(Count(_photoKind), Is.EqualTo(1));
        }

        /// <summary>Checks the photo flight phases: countdown only after arrival, taps accepted only while open, board stays blocked until the return ends.</summary>
        [UnityTest]
        public IEnumerator PhotoFlightPhasesGateCountdownAndTaps()
        {
            _data.Presentation.BoosterPhotoFlightDuration = 0.3f;
            _data.Presentation.BoosterPhotoLabelsFadeDuration = 0.1f;
            yield return StartAttempt();
            var boosters = _view.Boosters;
            var view = _view.GetComponent<AnomalyBoosterView>();
            var polaroid = _view.transform.Find("Polaroid").gameObject;
            Assert.That(Use(_photoKind), Is.True);
            Assert.That(boosters.PhotoPhase, Is.EqualTo(AnomalyPhotoPhase.Opening));
            Assert.That(polaroid.activeSelf, Is.False, "The photo leaves its place in the thumbnail together with its frame.");
            Assert.That(Photo().transform.Find("BoosterPhotoFlight"), Is.Not.Null, "The HUD frame itself flies, not a substitute.");
            view.OnPointerClick(null);
            boosters.Tick(2);
            Assert.That(boosters.PhotoPhase, Is.EqualTo(AnomalyPhotoPhase.Opening), "Taps during the arrival are ignored.");
            Assert.That(boosters.PhotoRemaining, Is.EqualTo(AnomalyBoosterController.PhotoDuration), "No countdown before arrival.");
            var deadline = Time.realtimeSinceStartup + 3;
            while (boosters.PhotoPhase == AnomalyPhotoPhase.Opening && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(boosters.PhotoPhase, Is.EqualTo(AnomalyPhotoPhase.Showing));
            Assert.That(Photo().transform.Find("BoosterPhotoFlight"), Is.Not.Null);
            view.OnPointerClick(null);
            Assert.That(boosters.PhotoPhase, Is.EqualTo(AnomalyPhotoPhase.Closing));
            Assert.That(boosters.IsPhotoOpen, Is.True, "The board stays blocked during the return flight.");
            view.OnPointerClick(null);
            Assert.That(boosters.PhotoPhase, Is.EqualTo(AnomalyPhotoPhase.Closing));
            deadline = Time.realtimeSinceStartup + 3;
            while (boosters.IsPhotoOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(boosters.IsPhotoOpen, Is.False);
            Assert.That(Photo().activeSelf, Is.False);
            Assert.That(polaroid.activeSelf, Is.True, "The thumbnail frame returns exactly when the photo lands.");
            yield return null;
            Assert.That(Photo().transform.Find("BoosterPhotoFlight"), Is.Null, "No flight overlay remains.");
        }

        /// <summary>Checks current unfound targeting, pulse, target cleanup, empty-stage stock preservation and passive thumbnail input.</summary>
        [UnityTest]
        public IEnumerator HintTargetAnimationAndPassiveThumbnail()
        {
            yield return StartAttempt();
            Assert.That(Use(_hintKind), Is.True);
            var target = _view.Boosters.HintTarget;
            Assert.That(target.Id, Is.EqualTo("a"));
            Assert.That(Use(_hintKind), Is.False);
            Assert.That(Count(_hintKind), Is.EqualTo(4));
            var finger = _view.transform.Find("BoardArea/Original/HintFinger").GetComponent<Image>();
            Assert.That(finger.rectTransform.anchorMin, Is.EqualTo(target.Bounds.center));
            Assert.That(finger.raycastTarget, Is.False);
            yield return new WaitForSeconds(0.15f);
            Assert.That(finger.enabled, Is.True);
            Assert.That(finger.rectTransform.localScale.x, Is.LessThan(1));
            var scale = finger.rectTransform.localScale;
            Set(_level, "IsTimerPaused", true);
            yield return new WaitForSeconds(0.1f);
            Assert.That(finger.rectTransform.localScale, Is.EqualTo(scale));
            Assert.That(Use(_hintKind), Is.False);
            Set(_level, "IsTimerPaused", false);
            Assert.That(_search.TryFind(target.Bounds.center), Is.True);
            Assert.That(_view.Boosters.HintTarget, Is.Null);
            Assert.That(finger.enabled, Is.False, "Pointer must clear on acceptance, before the finding flight.");
            Assert.That(Use(_hintKind), Is.True);
            Assert.That(_view.Boosters.HintTarget.Id, Is.EqualTo("b"));
            _search.TryFind(_data.AnomaliesList[1].Bounds.center);
            Assert.That(_view.Boosters.HintTarget, Is.Null);
            var count = Count(_hintKind);
            Assert.That(Use(_hintKind), Is.False);
            Assert.That(Count(_hintKind), Is.EqualTo(count));
            yield return NextStage();
            Assert.That(_search.StageIndex, Is.EqualTo(1));
            Assert.That(Use(_hintKind), Is.True);
            Assert.That(_view.Boosters.HintTarget.Id, Is.EqualTo("c"));
            var thumbnail = _view.GetComponentsInChildren<Image>(true).First(image => image.name == "OriginalThumbnail");
            Assert.That(thumbnail.raycastTarget, Is.True);
            var requests = 0;
            _view.PhotoRequested += () => requests++;
            _view.OnPointerClick(new PointerEventData(_eventRoot.GetComponent<EventSystem>())
                { pointerCurrentRaycast = new RaycastResult { gameObject = thumbnail.gameObject } });
            Assert.That(requests, Is.EqualTo(1), "A tap on the HUD photo requests the photo booster.");
            Assert.That(_view.Boosters.IsPhotoOpen, Is.False, "Spending stays with the economy, not the view.");
            Assert.That(Count(_photoKind), Is.EqualTo(5));
            var old = _view.Boosters;
            Call(_level, "ClearLevel");
            Assert.That(old.HintTarget, Is.Null);
            Assert.That(old.CanHint, Is.False);
            Assert.That(old.CanPhoto, Is.False);
        }

        /// <summary>Verifies the production HUD binds only two boosters, updates icons, and follows active-effect/pause/stock gates.</summary>
        [UnityTest]
        public IEnumerator HudShowsTwoEffectsAndRefreshesAvailability()
        {
            var boosters = (IList)Get(_system, "Boosters");
            Assert.That(boosters.Count, Is.EqualTo(2));
            _hudRoot = new GameObject("BoosterTestHud");
            var buttonType = Find("Game.UI.Hud.BoosterButtonView");
            var buttons = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(buttonType));
            for (var i = 0; i < 3; i++)
            {
                var obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Hud/BoosterButton.prefab"), _hudRoot.transform);
                buttons.Add(obj.GetComponent(buttonType));
            }
            var model = Activator.CreateInstance(Find("Game.UI.Hud.HudModel"));
            Set(model, "Boosters", boosters);
            SetCallback(model, "GetBoosterCountFunc", kind => Count(kind));
            SetCallback(model, "CanUseBoosterFunc", kind => Call(_system, "CanUse", kind));
            SetCallback(model, "IsBoosterUnlockedFunc", kind => true);
            SetCallback(model, "UseBoosterAction", kind => Use(kind));
            var controller = Activator.CreateInstance(Find("Game.UI.Hud.BoosterButtonsController"), buttons, model, Get(model, "IsBoosterUnlockedFunc"));
            Call(controller, "Initialize");
            Assert.That(((Component)buttons[2]).gameObject.activeSelf, Is.False);
            Assert.That(((Component)buttons[0]).name, Is.EqualTo("Hint"));
            Assert.That(((Component)buttons[1]).name, Is.EqualTo("Photo"));
            var icon = ((Component)buttons[0]).transform.Find("UnlockedContainer/Icon").GetComponent<Image>();
            Assert.That(icon.sprite.name, Is.EqualTo("Finger"));
            Call(controller, "RefreshButtonsInteractable");
            Assert.That(IsButtonEnabled(buttons[0]), Is.False);
            yield return StartAttempt();
            Call(controller, "RefreshButtonsInteractable");
            Assert.That(IsButtonEnabled(buttons[0]), Is.True);
            Assert.That(IsButtonEnabled(buttons[1]), Is.True);
            Use(_photoKind);
            Call(controller, "RefreshButtonsInteractable");
            Assert.That(IsButtonEnabled(buttons[0]), Is.False);
            Assert.That(IsButtonEnabled(buttons[1]), Is.False);
            _view.Boosters.Tick(5);
            Set(_level, "IsTimerPaused", true);
            Call(controller, "RefreshButtonsInteractable");
            Assert.That(IsButtonEnabled(buttons[0]), Is.False);
            Set(_level, "IsTimerPaused", false);
            Call(_system, "Add", _hintKind, -Count(_hintKind));
            Call(controller, "RefreshButtonsInteractable");
            Assert.That(IsButtonEnabled(buttons[0]), Is.True, "Empty stock must still allow the existing purchase window.");
            Assert.That(Use(_hintKind), Is.False);
            Call(_system, "Add", _hintKind, 3);
            Assert.That(Use(_hintKind), Is.True, "Existing reward/add path must replenish spendable stock.");
            yield return null;
        }

        /// <summary>Reads the actual Unity button state supplied by the production HUD adapter.</summary>
        private static bool IsButtonEnabled(object button)
        {
            return ((Component)button).GetComponent<Button>().interactable;
        }

        /// <summary>Adapts a local callback to the runtime enum contract without referencing Assembly-CSharp.</summary>
        private static void SetCallback(object model, string name, Func<object, object> callback)
        {
            var type = model.GetType().GetField(name, Flags)?.FieldType ?? model.GetType().GetProperty(name, Flags).PropertyType;
            var signature = type.GetMethod("Invoke");
            var argument = Expression.Parameter(signature.GetParameters()[0].ParameterType);
            Expression body = Expression.Invoke(Expression.Constant(callback), Expression.Convert(argument, typeof(object)));
            body = signature.ReturnType == typeof(void) ? Expression.Block(body, Expression.Empty()) : Expression.Convert(body, signature.ReturnType);
            Set(model, name, Expression.Lambda(type, body, argument).Compile());
        }

        /// <summary>Initializes only the existing attempt adapter and view, synchronously awaiting its completed preparation.</summary>
        private void PrepareAttempt()
        {
            var preparation = Call(_level, "InitializeSearch", _levelData, _progress, 1);
            Call(Call(preparation, "GetAwaiter"), "GetResult");
            Set(_level, "IsTimerPaused", false);
            _view = _root.GetComponentInChildren<AnomalySearchView>();
            _search = (AnomalySearchSystem)Get(Get(_level, "AnomalyLevel"), "Search");
        }

        /// <summary>Starts through the production Ready button with zero fixture-only introduction duration.</summary>
        private IEnumerator StartAttempt()
        {
            _view.GetComponentInChildren<Button>().onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 2;
            while (!_search.IsStarted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_search.IsStarted, Is.True);
        }

        /// <summary>Waits for the actual presentation acknowledgement, with a bounded deadline independent of coroutine frame scheduling.</summary>
        private IEnumerator NextStage()
        {
            var deadline = Time.realtimeSinceStartup + 2;
            while (_search.StageIndex == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_search.StageIndex, Is.EqualTo(1));
        }

        /// <summary>Returns the photo overlay, including when hidden.</summary>
        private GameObject Photo()
        {
            return _view.transform.Find("BoosterPhoto").gameObject;
        }

        /// <summary>Executes production economy guards and application.</summary>
        private bool Use(object kind)
        {
            return (bool)Call(_system, "TryUse", kind);
        }

        /// <summary>Reads the local profile inventory through its production public API.</summary>
        private int Count(object kind)
        {
            return (int)Call(_system, "GetCount", kind);
        }

        /// <summary>Resolves Assembly-CSharp types without a runtime assembly dependency.</summary>
        private static Type Find(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
        }

        /// <summary>Reads explicit public contracts or fixture-owned private state.</summary>
        private static object Get(object target, string name)
        {
            var field = target.GetType().GetField(name, Flags);
            return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Flags).GetValue(target);
        }

        /// <summary>Sets only isolated instances; production assets are never rewritten by tests.</summary>
        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Flags);
            if (field != null)
                field.SetValue(target, value);
            else
                target.GetType().GetProperty(name, Flags).SetValue(target, value);
        }

        /// <summary>Invokes a public production operation by its unique name/argument count.</summary>
        private static object Call(object target, string name, params object[] args)
        {
            return target.GetType().GetMethods(Flags).Single(method => method.Name == name && method.GetParameters().Length == args.Length)
                .Invoke(target, args);
        }
    }
}
#endif
