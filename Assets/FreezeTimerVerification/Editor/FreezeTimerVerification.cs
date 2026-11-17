using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.Gameplay;
using Game.Levels;
using Game.SFX;
using Game.UI.Buttons;
using Game.UI.Hud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FreezeTimerVerification
{
    private const string HudPath = "Assets/Game/UI/Hud/HudView.prefab";

    /// <summary>Adds the third nested booster button while preserving existing prefab overrides.</summary>
    [MenuItem("Tools/Verification/Freeze Timer Setup")]
    public static void Setup()
    {
        var root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var hud = new SerializedObject(root.GetComponent<HudView>());
            var buttons = hud.FindProperty("_boosterButtons");
            Check(buttons.arraySize >= 2, "Expected two existing buttons");
            if (buttons.arraySize == 3)
                UnityEngine.Object.DestroyImmediate(((BoosterButtonView)buttons.GetArrayElementAtIndex(2).objectReferenceValue).gameObject);
            var source = (BoosterButtonView)buttons.GetArrayElementAtIndex(1).objectReferenceValue;
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Hud/BoosterButton.prefab"), source.transform.parent);
            PrefabUtility.SetPropertyModifications(copy, PrefabUtility.GetPropertyModifications(source.gameObject));
            copy.name = "FreezeTimerBoosterButton";
            copy.transform.SetAsLastSibling();
            var tutorial = new SerializedObject(copy.GetComponent<TutorialButton>());
            tutorial.FindProperty("_buttonId").stringValue = "FreezeTimer";
            tutorial.ApplyModifiedPropertiesWithoutUndo();
            var config = AssetDatabase.LoadAssetAtPath<FreezeTimerBoosterData>("Assets/Game/Boosters/FreezeTimer/FreezeTimerBoosterData.asset");
            copy.transform.Find("UnlockedContainer/Icon").GetComponent<Image>().sprite = config.Icon;
            buttons.arraySize = 3;
            buttons.GetArrayElementAtIndex(2).objectReferenceValue = copy.GetComponent<BoosterButtonView>();
            hud.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Debug.Log("FREEZE_SETUP PASSED: three ordered buttons, unique tutorial ID, freeze icon");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Exercises the production HUD callback and original freeze duration on the production level prefab.</summary>
    [MenuItem("Tools/Verification/Freeze Timer Run")]
    public static async void Run()
    {
        GameObject root = null;
        GameObject hudRoot = null;
        GameObject audioRoot = null;
        var previousBackground = Application.runInBackground;
        var previousScale = Time.timeScale;
        try
        {
            Check(EditorApplication.isPlaying, "PlayMode required");
            Application.runInBackground = true;
            Time.timeScale = 1;
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Levels/LevelController.prefab"));
            var level = root.GetComponent<LevelController>();
            Set(level, "_levelTimer", 1000f);
            Set(level, "_isStarted", true);
            audioRoot = new GameObject("FreezeVerificationAudio");
            var manager = audioRoot.AddComponent<SfxManager>();
            manager.Construct(AssetDatabase.LoadAssetAtPath<SfxData>("Assets/Game/SFX/SfxData.asset"));
            var audio = (AudioSystem)FormatterServices.GetUninitializedObject(typeof(AudioSystem));
            Set(audio, "_sfxManager", manager);
            Set(level, "_audioSystem", audio);
            var levels = (LevelsManager)FormatterServices.GetUninitializedObject(typeof(LevelsManager));
            Set(levels, "_levelController", level);
            var data = AssetDatabase.LoadAssetAtPath<FreezeTimerBoosterData>("Assets/Game/Boosters/FreezeTimer/FreezeTimerBoosterData.asset");
            var booster = new FreezeTimerBooster(levels, data, audio);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            var buttons = (List<BoosterButtonView>)Get(prefab.GetComponent<HudView>(), "_boosterButtons");
            Check(buttons.Count == 3, "Three HUD references");
            hudRoot = UnityEngine.Object.Instantiate(buttons[2].gameObject);
            var gameButton = hudRoot.GetComponent<GameButton>();
            Set(gameButton, "_audioSystem", audio);
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<Game.UI.Buttons.Signals.ButtonClickedSignal>().OptionalSubscriber();
            Set(gameButton, "_signalBus", container.Resolve<SignalBus>());
            var selected = BoosterKind.None;
            var model = new HudModel
            {
                Boosters = new List<Booster> { booster },
                GetBoosterCountFunc = kind => 2,
                UseBoosterAction = kind => { selected = kind; booster.Apply(); }
            };
            var controller = new BoosterButtonsController(new List<BoosterButtonView> { hudRoot.GetComponent<BoosterButtonView>() }, model, kind => true);
            controller.Initialize();
            hudRoot.GetComponent<Button>().onClick.Invoke();
            Check(selected == BoosterKind.FreezeTimer && level.IsTimerPaused, "HUD callback applies freeze");
            var time = level.TimerValue;
            await UniTask.Delay(500);
            Check(level.IsTimerPaused && Mathf.Approximately(time, level.TimerValue), "Timer remains frozen");
            await UniTask.Delay(TimeSpan.FromSeconds(data.Duration));
            Check(!level.IsTimerPaused, "Original duration restores timer");
            await UniTask.Delay(100);
            Check(level.TimerValue < time, "Timer resumes countdown");
            Check(AssetDatabase.LoadAssetAtPath<SfxData>("Assets/Game/SFX/SfxData.asset").GetPrefab("TimeFreeze") != null, "Freeze sound reference");
            Debug.Log("FREEZE_RUNTIME PASSED: HUD callback, frozen countdown, original duration, resumed countdown, SFX reference");
        }
        catch (Exception exception)
        {
            Debug.LogError("FREEZE_RUNTIME FAILED: " + exception);
        }
        finally
        {
            if (hudRoot != null) UnityEngine.Object.Destroy(hudRoot);
            if (root != null) UnityEngine.Object.Destroy(root);
            if (audioRoot != null) UnityEngine.Object.Destroy(audioRoot);
            Application.runInBackground = previousBackground;
            Time.timeScale = previousScale;
        }
    }

    /// <summary>Reads fixture wiring without modifying production APIs.</summary>
    private static object Get(object owner, string name)
    {
        return owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }

    /// <summary>Supplies isolated fixture dependencies.</summary>
    private static void Set(object owner, string name, object value)
    {
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    }

    /// <summary>Reports a failed acceptance observation.</summary>
    private static void Check(bool condition, string criterion)
    {
        if (!condition) throw new Exception(criterion);
    }
}
