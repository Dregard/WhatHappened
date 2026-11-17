#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using Game.Boosters;
using Game.Gameplay.Chapters;
using Game.Levels;
using Game.Purchase;
using Game.UI.Hud;
using Game.UI.Windows.BoosterWindow;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Disposable playtest harness: builds production views with fake data and saves 1080x1920 screenshots.</summary>
public sealed class UiShotRunner : MonoBehaviour
{
    private const string Art = "Assets/Game/Anomalies/Theme/Art/";
    private string _dir;
    private Camera _cam;
    private RenderTexture _rt;
    private Canvas _hudCanvas;
    private Canvas _windowsCanvas;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    private IEnumerator Start()
    {
        _dir = Application.dataPath + "/__OrchestratorTemp-e7de6796649b4f79b2d360805a552b43/Shots";
        Directory.CreateDirectory(_dir);
        yield return Run();
        File.WriteAllText(_dir + "/done.txt", "done");
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Run()
    {
        _rt = new RenderTexture(1080, 1920, 24);
        var camObject = new GameObject("ShotCamera");
        _cam = camObject.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.black;
        _cam.targetTexture = _rt;
        _hudCanvas = MakeCanvas("Hud", 0);
        _windowsCanvas = MakeCanvas("Windows", 10);
        var theme = UiThemeData.LoadOrDefault();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TempAssets/Previews/02_Game_Find.png");
        var picture = Sprite.Create(texture, new Rect(45, 468, 990, 995), new Vector2(0.5f, 0.5f), 100);
        var data = new AnomalyLevelData
        {
            MainImage = picture,
            ClueImage = Load("ev_testtube"),
            ClueName = "A strange green test tube",
            IntroText = "Look closely. Something happened here...",
        };
        for (var i = 0; i < 3; i++)
        {
            var tex = new Texture2D(8, 8);
            var color = i == 0 ? Color.magenta : i == 1 ? Color.blue : Color.green;
            tex.SetPixels(Enumerable.Repeat(color, 64).ToArray());
            tex.Apply();
            data.AnomaliesList.Add(new AnomalyData
            {
                Id = "a" + i, Image = Sprite.Create(tex, new Rect(0, 0, 8, 8), Vector2.one * 0.5f),
                Bounds = new Rect(0.2f + 0.25f * i, 0.5f, 0.12f, 0.12f), HudText = "Someone swapped the green potion!",
            });
        }
        data.StagesList.Add(new AnomalyStageData { Count = 3, AnomalyIdsList = new List<string> { "a0", "a1", "a2" } });
        var search = new AnomalySearchSystem(data, 1) { IsPaused = false };
        var view = AnomalySearchView.Create(_hudCanvas, data, search);
        view.Ready += search.Start;

        var hudRoot = new GameObject("ChapterHud", typeof(RectTransform));
        hudRoot.transform.SetParent(_hudCanvas.transform, false);
        AnomalyUi.Bounds((RectTransform)hudRoot.transform, Vector2.zero, Vector2.one);
        var chapterHud = hudRoot.AddComponent<ChapterHudView>();
        chapterHud.Initialize("Laboratory", () => { });
        chapterHud.Show(1, 3, true);
        var model = new HudModel
        {
            GetBoosterCountFunc = kind => kind == BoosterKind.Compass ? 5 : 3,
            CanUseBoosterFunc = kind => true,
            IsBoosterUnlockedFunc = kind => true,
            UseBoosterAction = kind => { },
            IsSearchStartedFunc = () => search.IsStarted,
        };
        var layout = HudLayoutView.Create(_hudCanvas, theme, model);
        var gold = layout.transform.Find("GoldValue");
        if (gold != null)
            gold.GetComponent<TMP_Text>().text = "999";
        yield return null;
        yield return null;
        yield return Shot("01_level_start");

        view.transform.Find("Ready").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSeconds(1f);
        yield return Shot("02_game_find");

        search.TryFind(data.AnomaliesList[0].Bounds.center);
        yield return new WaitForSeconds(1.2f);
        yield return Shot("03_found");

        view.Boosters.TryHint(new AnomalyHintData { FingerSprite = Load("kit_finger"), FingerSize = new Vector2(110, 110), Fingertip = new Vector2(0.2f, 0.95f) }, 30f);
        yield return new WaitForSeconds(0.3f);
        yield return Shot("05_hint");

        view.Boosters.TryPhoto();
        yield return new WaitForSeconds(0.5f);
        yield return Shot("04_photo");
        view.Boosters.Tick(10);
        yield return null;

        yield return PopupShot("06a_out_of_hints", BoosterKind.Compass, 100);
        yield return PopupShot("06b_out_of_photos", BoosterKind.FreezeTimer, 50);

        var levels = ScriptableObject.CreateInstance<LevelsData>();
        levels.AllEvidenceDuration = 2.2f;
        levels.CulpritRevealDuration = 0f;

        // Investigation window
        var inv = NewWindow();
        var invModel = Model(levels, ChapterWindowModeAlias.Investigation, new[] { Load("ev_footprint"), Load("ev_testtube"), null }, false);
        invModel.CloseAction = () => { };
        inv.Setup(invModel);
        yield return new WaitForSeconds(0.2f);
        yield return Shot("14_investigation");
        Destroy(inv.gameObject);

        // Non-final level result
        var win = NewWindow();
        var m1 = Model(levels, ChapterWindowModeAlias.EvidenceFound, new[] { Load("ev_footprint"), null, null }, false);
        m1.NewClueIndex = 1;
        m1.NewClue = Load("ev_testtube");
        m1.NewClueName = "A strange green test tube";
        win.Setup(m1);
        yield return new WaitForSeconds(0.3f);
        yield return Shot("07_evidence");
        Click(win, "NEXT");
        yield return new WaitForSeconds(1.3f);
        yield return Shot("08_reward");
        Click(win, "GET");
        yield return null;
        Destroy(win.gameObject);

        // Final level result
        var fin = NewWindow();
        var m2 = Model(levels, ChapterWindowModeAlias.EvidenceFound, new[] { Load("ev_footprint"), Load("ev_testtube"), null }, true);
        m2.NewClueIndex = 2;
        m2.NewClue = Load("ev_glasses");
        m2.NewClueName = "Round glasses";
        fin.Setup(m2);
        yield return new WaitForSeconds(0.3f);
        Click(fin, "NEXT");
        yield return new WaitForSeconds(0.85f);
        yield return Shot("09_all_evidence");
        yield return new WaitForSeconds(0.9f);
        yield return Shot("10_all_evidence_vfx");
        yield return new WaitForSeconds(1.9f);
        yield return Shot("11_culprit_reveal");
        Click(fin, "TapCatcher");
        yield return new WaitForSeconds(0.4f);
        yield return Shot("12_culprit_scene");
        Click(fin, "NEXT");
        yield return new WaitForSeconds(0.4f);
        yield return Shot("13_chapter_reward");
    }

    private IEnumerator PopupShot(string name, BoosterKind kind, int price)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Windows/BoosterWindow/BoosterWindow.prefab");
        var instance = Instantiate(prefab, _windowsCanvas.transform);
        var rect = (RectTransform)instance.transform;
        AnomalyUi.Bounds(rect, Vector2.zero, Vector2.one);
        var reward = ScriptableObject.CreateInstance<BoosterRewardData>();
        typeof(BoosterRewardData).GetField("_amount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(reward, 3);
        typeof(BoosterRewardData).GetField("_currencyKind", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(reward, kind);
        var cost = ScriptableObject.CreateInstance<CurrencyPriceData>();
        typeof(CurrencyPriceData).GetField("_amount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cost, price);
        var window = instance.GetComponent<BoosterWindow>();
        window.Setup(new BoosterWindowModel
        {
            CanBuyBooster = true,
            BoosterViewModel = new BoosterViewModel
            {
                Name = kind == BoosterKind.Compass ? "Hint" : "Photo",
                Description = kind == BoosterKind.Compass ? "Stuck? A hint shows where to look." : "Peek at the original once more!",
                Price = cost, Reward = reward,
            },
        });
        yield return new WaitForSeconds(0.2f);
        yield return Shot(name);
        Destroy(instance);
        yield return null;
    }

    private ChapterWindow NewWindow()
    {
        var go = new GameObject("ChapterWindow", typeof(RectTransform));
        go.transform.SetParent(_windowsCanvas.transform, false);
        AnomalyUi.Bounds((RectTransform)go.transform, Vector2.zero, Vector2.one);
        return go.AddComponent<ChapterWindow>();
    }

    private static ChapterWindowModel Model(LevelsData levels, ChapterWindowModeAlias mode, Sprite[] clues, bool isFinal)
    {
        var coin = UiThemeData.LoadOrDefault().CoinIcon;
        var portrait = MakeSolidSprite(new Color(0.9f, 0.3f, 0.25f));
        var scene = MakeSolidSprite(new Color(0.8f, 0.65f, 0.4f));
        return new ChapterWindowModel
        {
            Mode = mode, CluesArray = clues, IsFinal = isFinal, PresentationData = levels,
            LevelRewardsList = new List<ChapterRewardModel> { new ChapterRewardModel { Icon = coin, Text = "+200" } },
            ChapterRewardsList = new List<ChapterRewardModel> { new ChapterRewardModel { Icon = coin, Text = "+1000", IconCount = 3 } },
            CulpritPortrait = portrait, CulpritCaption = "The octopus chef did it!",
            CulpritSceneImage = scene, CulpritSceneCaption = "He mixed the potions for his new recipe!",
            CommitClue = () => UniTask.CompletedTask,
            ClaimLevelReward = () => UniTask.CompletedTask,
            ClaimChapterReward = () => UniTask.CompletedTask,
            FinishAction = () => { },
        };
    }

    private static Sprite MakeSolidSprite(Color color)
    {
        var tex = new Texture2D(16, 16);
        tex.SetPixels(Enumerable.Repeat(color, 256).ToArray());
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);
    }

    private static void Click(Component root, string name)
    {
        var button = root.GetComponentsInChildren<Button>(true).FirstOrDefault(item => item.name == name);
        if (button == null)
            Debug.LogWarning("Missing button " + name);
        else
            button.onClick.Invoke();
    }

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    }

    private Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _cam;
        canvas.planeDistance = 10;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0;
        return canvas;
    }

    private IEnumerator Shot(string name)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        _cam.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = _rt;
        var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes($"{_dir}/{name}.png", tex.EncodeToPNG());
        Destroy(tex);
    }
}
#endif
