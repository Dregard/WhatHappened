using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Anomalies.Tests
{
    public sealed class AnomalyPresentationTests
    {
        private GameObject _root;
        private GameObject _eventRoot;
        private Texture2D _texture;
        private Sprite _sprite;
        private AnomalyLevelData _data;
        private AnomalySearchSystem _search;
        private AnomalySearchView _view;
        private Canvas _canvas;

        /// <summary>Builds only an in-memory UI fixture; no startup, production services or transports are invoked.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PresentationTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _canvas = _root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _eventRoot = new GameObject("PresentationInput", typeof(EventSystem));
            _texture = new Texture2D(20, 10);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 20, 10), Vector2.one * 0.5f);
            _data = new AnomalyLevelData { MainImage = _sprite, FindingDuration = 0.25f, TransitionDuration = 0.15f };
            _data.Presentation.PhotoDuration = 0.15f;
            _data.Presentation.FadeDuration = 0.15f;
            _data.Presentation.FlightDuration = 0.3f;
            _data.Presentation.StarsDuration = 0.15f;
            _data.Presentation.BlackoutDuration = 0.12f;
            _data.AnomaliesList.Add(new AnomalyData { Id = "a", Image = _sprite, Bounds = new Rect(0.05f, 0.1f, 0.25f, 0.3f), HudText = "First" });
            _data.AnomaliesList.Add(new AnomalyData { Id = "b", Image = _sprite, Bounds = new Rect(0.65f, 0.6f, 0.3f, 0.3f), HudText = "Second" });
            _data.AnomaliesList.Add(new AnomalyData { Id = "c", Image = _sprite, Bounds = new Rect(0.35f, 0.3f, 0.2f, 0.2f), HudText = "Last" });
            _data.StagesList.Add(new AnomalyStageData { Count = 2, AnomalyIdsList = new List<string> { "a", "b" } });
            _data.StagesList.Add(new AnomalyStageData { Count = 1, AnomalyIdsList = new List<string> { "c" } });
            CreateAttempt();
        }

        /// <summary>Releases the fixture without modifying loaded project scenes or shared assets.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_eventRoot);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        /// <summary>Checks photo gating, concurrent findings, outline-before-fade, arrival-before-reveal and hidden stage replacement.</summary>
        [UnityTest]
        public IEnumerator ConcurrentFindingsAndBlackoutRespectPresentationGates()
        {
            var cuesList = new List<string>();
            _view.SfxRequested += cuesList.Add;
            var completed = 0;
            _search.Completed += () => completed++;
            _view.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(_search.IsStarted, Is.False, "Photo must finish before search starts.");
            yield return Until(() => _search.IsStarted);
            Assert.That(FindImage("OriginalThumbnail").enabled, Is.True);
            Assert.That(cuesList, Does.Contain(_data.Presentation.PhotoSfx));
            Assert.That(_view.transform.Find("Slots").childCount, Is.EqualTo(2));
            var a = FindImage("a");
            var b = FindImage("b");
            var aSlot = FindImage("Closed_a");
            var bSlot = FindImage("Closed_b");
            Assert.That(aSlot.sprite, Is.Null);
            Assert.That(_search.TryFind(_data.AnomaliesList[0].Bounds.center), Is.True);
            Assert.That(_search.TryFind(_data.AnomaliesList[1].Bounds.center), Is.True);
            Assert.That(_search.TryFind(_data.AnomaliesList[0].Bounds.center), Is.False);
            Assert.That(a.color.a, Is.EqualTo(1));
            Assert.That(b.color.a, Is.EqualTo(1));
            Assert.That(aSlot.sprite, Is.Null);
            Assert.That(bSlot.sprite, Is.Null);
            Assert.That(_search.IsTransitionPending, Is.False);
            Assert.That(cuesList.FindAll(id => id == _data.Presentation.FindingSfx).Count, Is.EqualTo(2));
            var marker = a.GetComponentInChildren<AnomalyMarkerGraphic>();
            while (marker.Progress < 1)
            {
                Assert.That(a.color.a, Is.EqualTo(1), "Layer faded before outline finished.");
                Assert.That(aSlot.sprite, Is.Null, "HUD opened before flight.");
                yield return null;
            }
            yield return Until(() => _search.IsTransitionPending);
            Assert.That(aSlot.sprite, Is.EqualTo(_sprite));
            Assert.That(bSlot.sprite, Is.EqualTo(_sprite));
            Assert.That(aSlot.transform.GetChild(0).gameObject.activeSelf, Is.False);
            Assert.That(a.gameObject.activeSelf, Is.False);
            Assert.That(completed, Is.Zero);
            var veil = FindImage("ShutterAndBlackout");
            var sawHiddenSwap = false;
            while (_search.StageIndex == 0)
            {
                if (_view.transform.Find("Slots/Closed_c") != null)
                {
                    sawHiddenSwap = true;
                    Assert.That(veil.color.a, Is.EqualTo(1).Within(0.0001f));
                    Assert.That(_search.TryFind(_data.AnomaliesList[2].Bounds.center), Is.False);
                    break;
                }
                yield return null;
            }
            Assert.That(sawHiddenSwap, Is.True);
            yield return Until(() => _search.StageIndex == 1);
            Assert.That(veil.color.a, Is.Zero);
            yield return null;
            Assert.That(_view.transform.Find("Slots").childCount, Is.EqualTo(1));
            Assert.That(_search.TryFind(_data.AnomaliesList[2].Bounds.center), Is.True);
            Assert.That(completed, Is.Zero);
            yield return Until(() => _search.IsTransitionPending);
            Assert.That(completed, Is.Zero, "Final finding must retain the transition gate.");
            Assert.That(FindImage("Closed_c").sprite, Is.EqualTo(_sprite));
            yield return Until(() => _search.IsCompleted);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(veil.color.a, Is.Zero);
        }

        /// <summary>Exercises scaled rectangular input, UI exclusion, misses, pause and cancellation into a fresh attempt.</summary>
        [UnityTest]
        public IEnumerator ScaledInputMissesPauseAndRestartAreIsolated()
        {
            _data.Presentation.PhotoDuration = 0;
            _view.GetComponentInChildren<Button>().onClick.Invoke();
            yield return null;
            _view.transform.localScale = new Vector3(0.7f, 0.8f, 1);
            Canvas.ForceUpdateCanvases();
            var board = FindImage("Original");
            var a = FindImage("a");
            var bounds = _data.AnomaliesList[0].Bounds;
            var visual = AnomalyUi.LocalRect(a.rectTransform, board.rectTransform);
            Assert.That(visual.min.x, Is.EqualTo(board.rectTransform.rect.xMin + bounds.xMin * board.rectTransform.rect.width).Within(0.01f));
            Click(board, new Vector2(0.5f, 0.95f), FindImage("OriginalThumbnail").gameObject);
            Assert.That(board.transform.Find("Miss"), Is.Null, "UI taps must not count as misses.");
            Click(board, new Vector2(0.5f, 0.95f), board.gameObject);
            Assert.That(board.transform.Find("Miss"), Is.Not.Null);
            Click(board, bounds.center, board.gameObject);
            Assert.That(_search.IsFound("a"), Is.True);
            Click(board, _data.AnomaliesList[1].Bounds.center, board.gameObject);
            Assert.That(_search.IsStageFound, Is.True);
            var marker = a.GetComponentInChildren<AnomalyMarkerGraphic>();
            _search.IsPaused = true;
            var progress = marker.Progress;
            yield return null;
            yield return null;
            Assert.That(marker.Progress, Is.EqualTo(progress));
            Assert.That(a.color.a, Is.EqualTo(1));
            var previous = _search;
            var oldView = _view;
            oldView.gameObject.SetActive(false);
            Object.Destroy(oldView.gameObject);
            previous.IsPaused = false;
            CreateAttempt();
            yield return new WaitForSeconds(0.8f);
            Assert.That(previous.IsTransitionPending, Is.False);
            Assert.That(_search.IsStarted, Is.False);
            Assert.That(_search.IsFound("a"), Is.False);
            Assert.That(_view.transform.Find("Slots").childCount, Is.Zero);
        }

        /// <summary>Checks that the board frame hugs the fitted picture for landscape, portrait and square photos and that hit conversion still uses the picture rectangle.</summary>
        [UnityTest]
        public IEnumerator BoardFrameFollowsFittedPicture()
        {
            foreach (var size in new[] { new Vector2Int(20, 10), new Vector2Int(10, 20), new Vector2Int(10, 10), new Vector2Int(9, 16) })
            {
                var texture = new Texture2D(size.x, size.y);
                var sprite = Sprite.Create(texture, new Rect(0, 0, size.x, size.y), Vector2.one * 0.5f);
                Object.DestroyImmediate(_view.gameObject);
                _data.MainImage = sprite;
                CreateAttempt();
                yield return null;
                Canvas.ForceUpdateCanvases();
                var picture = AnomalyUi.LocalRect(FindImage("Original").rectTransform, (RectTransform)_view.transform);
                var frame = AnomalyUi.LocalRect(FindImage("BoardFrame").rectTransform, (RectTransform)_view.transform);
                var padding = frame.size - picture.size;
                Assert.That(picture.width / picture.height, Is.EqualTo((float)size.x / size.y).Within(0.01f), size.ToString());
                Assert.That(picture.xMin - frame.xMin, Is.EqualTo(30f).Within(0.5f), size.ToString());
                Assert.That(frame.xMax - picture.xMax, Is.EqualTo(32f).Within(0.5f), size.ToString());
                Assert.That(picture.yMin - frame.yMin, Is.EqualTo(39f).Within(0.5f), size.ToString());
                Assert.That(frame.yMax - picture.yMax, Is.EqualTo(24f).Within(0.5f), size.ToString());
                Assert.That(padding.x, Is.EqualTo(62f).Within(1f), size.ToString());
                Assert.That(FindImage("BoardFrame").transform.GetSiblingIndex(), Is.GreaterThan(FindImage("Original").transform.parent.GetSiblingIndex()), "Frame must draw above the picture.");
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Connects the same Ready contract as the production controller without application services.</summary>
        private void CreateAttempt()
        {
            _search = new AnomalySearchSystem(_data, 1);
            _view = AnomalySearchView.Create(_canvas,
                UnityEditor.AssetDatabase.LoadAssetAtPath<AnomalySearchView>("Assets/Game/Anomalies/AnomalySearch.prefab"), _data, _search);
            _view.Ready += _search.Start;
        }

        /// <summary>Finds a named fixture image, including passive hidden layers.</summary>
        private Image FindImage(string name)
        {
            foreach (var image in _view.GetComponentsInChildren<Image>(true))
                if (image.name == name)
                    return image;
            throw new InvalidOperationException(name);
        }

        /// <summary>Sends a pointer through the board's actual scaled screen-to-local mapping.</summary>
        private void Click(Image board, Vector2 normalized, GameObject raycastTarget)
        {
            var rect = board.rectTransform.rect;
            var world = board.rectTransform.TransformPoint(rect.min + normalized * rect.size);
            var input = new PointerEventData(_eventRoot.GetComponent<EventSystem>())
            {
                position = RectTransformUtility.WorldToScreenPoint(null, world),
                pointerCurrentRaycast = new RaycastResult { gameObject = raycastTarget }
            };
            _view.OnPointerClick(input);
        }

        /// <summary>Bounds fixture waits so a broken presentation reports a failure instead of hanging the runner.</summary>
        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 8;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(condition(), Is.True, "Presentation timed out.");
        }
    }
}
